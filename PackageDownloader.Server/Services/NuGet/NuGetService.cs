using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NuGet.Common;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using PackageDownloader.Server.Hubs;
using PackageDownloader.Service.Compress;
using PackageDownloader.Service.Interface;
using PackageDownloader.Shared;
using PackageDownloader.Shared.NuGet;
using PackageDownloader.Shared.Response;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PackageDownloader.NuGet
{
    public class NuGetService: IPackageService
    {
        private readonly IHubContext<DownloadPackageHub, IDownloadPackageHubClient> _downloadHubContext;
        private readonly ICompressService _compressService;
        private readonly IHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        private class PackageInfo
        {
            public string packageId { get; set; }
            public NuGetVersion packageVersion { get; set; }
        }

        public NuGetService(IHubContext<DownloadPackageHub, IDownloadPackageHubClient> hubContext, ICompressService compressService, IHostEnvironment environment, IConfiguration configuration)
        {
            _downloadHubContext = hubContext;
            _compressService = compressService;
            _environment = environment;
            _configuration = configuration;
        }

        public async Task<IEnumerable<string>> SearchPackageAsync(string searchPackageName, string repositoryUrl, bool includePrerelease)
        {
            ILogger logger = NullLogger.Instance;
            CancellationToken cancellationToken = CancellationToken.None;
            SourceCacheContext cache = new SourceCacheContext();
            SourceRepository repository = Repository.Factory.GetCoreV3(repositoryUrl);
            PackageSearchResource resource = await repository.GetResourceAsync<PackageSearchResource>();
            SearchFilter searchFilter = new SearchFilter(includePrerelease: includePrerelease);

            IEnumerable<IPackageSearchMetadata> results = await resource.SearchAsync(
                searchPackageName,
                searchFilter,
                skip: 0,
                take: 10,
                logger,
                cancellationToken);

            return results.Select(x => x.Identity.Id).AsEnumerable<string>();
        }

        public async Task<IEnumerable<string>> GetPackageVersionAsync(string packageId, string repositoryUrl, bool includePrerelease)
        {
            ILogger logger = NullLogger.Instance;
            CancellationToken cancellationToken = CancellationToken.None;
            SourceCacheContext cache = new SourceCacheContext();
            SourceRepository repository = Repository.Factory.GetCoreV3(repositoryUrl);
            FindPackageByIdResource resource = await repository.GetResourceAsync<FindPackageByIdResource>();

            IEnumerable<NuGetVersion> versions = await resource.GetAllVersionsAsync(
                packageId,
                cache,
                logger,
                cancellationToken);

            if (includePrerelease == true)
            {
                return versions.OrderByDescending(x => x).Select(x => x.ToNormalizedString()).AsEnumerable<string>();
            }
            else
            {
                return versions.Where(x => x.IsPrerelease == false).OrderByDescending(x => x).Select(x => x.ToNormalizedString()).AsEnumerable<string>();
            }
            
        }

        private async Task<PackageInfo> GetBestMatchPackageVersionsAsync(SourceRepository repository, string packageId, VersionRange range)
        {
            ILogger logger = NullLogger.Instance;
            CancellationToken cancellationToken = CancellationToken.None;
            SourceCacheContext cache = new SourceCacheContext();
            FindPackageByIdResource resource = await repository.GetResourceAsync<FindPackageByIdResource>();

            IEnumerable<NuGetVersion> versions = await resource.GetAllVersionsAsync(
                packageId,
                cache,
                logger,
                cancellationToken);

            NuGetVersion bestMatchVersion = range.FindBestMatch(versions);
            return new PackageInfo { packageId = packageId, packageVersion = bestMatchVersion };
        }

        public async Task<string> DownloadPackageAsync(string connectionID, RequestDownloadInfo requestInfo)
        {
            // cast 
            RequestDownloadNuGetInfo info = (RequestDownloadNuGetInfo)requestInfo;

            // server response
            ServerResponse response = new ServerResponse()
            {
                payload = new Dictionary<string, string>()
            };

            string _outputDirectory = $"{_environment.ContentRootPath}/wwwroot/{_configuration.GetValue<string>("DownloadPath")}";

            // check if output directory exists
            if (!Directory.Exists(_outputDirectory))
            {
                Directory.CreateDirectory(_outputDirectory);
            }

            string connectionSubName = $"{connectionID}-{DateTime.Now:yyyymmddHHmmss}";
            string connectionDirectory = $"{_outputDirectory}/{connectionSubName}";
            Directory.CreateDirectory(connectionDirectory);

            // send message
            response.payload.Clear();
            response.payload.Add("Resource", $"{connectionSubName} created.");
            await _downloadHubContext.Clients.Client(connectionID).Response(response);

            Queue<PackageInfo> _downloadQueue = new Queue<PackageInfo>();
            List<string> _cacheDownloadedFileName = new List<string>();

            ILogger logger = NullLogger.Instance;
            CancellationToken cancellationToken = CancellationToken.None;
            SourceCacheContext cache = new SourceCacheContext();
            SourceRepository repository = Repository.Factory.GetCoreV3(info.repository);
            FindPackageByIdResource resource = await repository.GetResourceAsync<FindPackageByIdResource>();

            // set all parent packages
            for (int i = 0; i < info.packageList.Count(); i++)
            {
                string packageId = info.packageList.ElementAt(i).packageId;
                string packageVersionValue = info.packageList.ElementAt(i).packageVersion;

                NuGetVersion packageVersion = new NuGetVersion(packageVersionValue);
                PackageInfo package = new PackageInfo { packageId = packageId, packageVersion = packageVersion };

                if (package.packageVersion != null)
                {
                    _downloadQueue.Enqueue(package);
                }
                else
                {
                    FloatRange floatRange = null;
                    if (info.preReleased == true)
                    {
                        // include pre-release
                        floatRange = new FloatRange(NuGetVersionFloatBehavior.AbsoluteLatest);
                    }
                    else
                    {
                        // released
                        floatRange = new FloatRange(NuGetVersionFloatBehavior.Major);
                    }
                    FloatRange fr = new FloatRange(NuGetVersionFloatBehavior.Major);
                    VersionRange range = new VersionRange(floatRange: fr);

                    package = await GetBestMatchPackageVersionsAsync(repository, packageId, range);
                }
                _downloadQueue.Enqueue(package);
            }

            // download counter
            int download_counter = 0;
            while (_downloadQueue.Count > 0)
            {
                PackageInfo package = _downloadQueue.Dequeue();

                string packageFilePath = $"{connectionDirectory}/{package.packageId}.{package.packageVersion}.nupkg";

                if (_cacheDownloadedFileName.Contains(packageFilePath))
                {
                    continue;
                }
                else
                {
                    _cacheDownloadedFileName.Add(packageFilePath);
                }

                using FileStream packageStream = new FileStream(packageFilePath, FileMode.Create);

                await resource.CopyNupkgToStreamAsync(
                    package.packageId,
                    package.packageVersion,
                    packageStream,
                    cache,
                    logger,
                    cancellationToken);

                download_counter++;

                Console.WriteLine($"Downloaded package {package.packageId} {package.packageVersion}");

                using PackageArchiveReader packageReader = new PackageArchiveReader(packageStream);
                NuspecReader nuspecReader = await packageReader.GetNuspecReaderAsync(cancellationToken);

                Console.WriteLine($"Tags: {nuspecReader.GetTags()}");
                Console.WriteLine($"Description: {nuspecReader.GetDescription()}");

                using PackageArchiveReader reader = new PackageArchiveReader(packageStream);
                NuspecReader nuspec = reader.NuspecReader;
                Console.WriteLine($"ID: {nuspec.GetId()}");
                Console.WriteLine($"Version: {nuspec.GetVersion()}");
                Console.WriteLine($"Description: {nuspec.GetDescription()}");
                Console.WriteLine($"Authors: {nuspec.GetAuthors()}");

                if (info.withDependency == false)
                {
                    Console.WriteLine("\nDependencies download is not need.");
                    continue;
                }

                Console.WriteLine("\nStart download dependencies:");
                foreach (var dependencyGroup in nuspec.GetDependencyGroups())
                {
                    Console.WriteLine($" - {dependencyGroup.TargetFramework.GetFrameworkString()}");

                    // check target framework
                    if (!info.targetFramework.Contains("all", StringComparer.InvariantCultureIgnoreCase)
                        && !info.targetFramework.Contains(dependencyGroup.TargetFramework.GetFrameworkString(), StringComparer.InvariantCultureIgnoreCase))
                    {
                        Console.WriteLine($" -- {dependencyGroup.TargetFramework.GetFrameworkString()} not match target framework.");
                        continue;
                    }

                    foreach (var dependency in dependencyGroup.Packages)
                    {
                        Console.WriteLine($"   > {dependency.Id} {dependency.VersionRange}");

                        PackageInfo dependencyPackage = await GetBestMatchPackageVersionsAsync(repository, dependency.Id, dependency.VersionRange);
                        Console.WriteLine($"   -- best match version: {dependency.Id} {dependencyPackage.packageVersion}");
                        _downloadQueue.Enqueue(dependencyPackage);
                    }
                }

                // check if send message is needed
                if (download_counter % 10 == 0)
                {
                    // send message
                    response.payload.Clear();
                    response.payload.Add("DownloadCounter", download_counter.ToString());
                    await _downloadHubContext.Clients.Client(connectionID).Response(response);
                }
            }

            // send message
            response.payload.Clear();
            response.payload.Add("DownloadCounter", download_counter.ToString());
            await _downloadHubContext.Clients.Client(connectionID).Response(response);

            string zipFileName = $"{_outputDirectory}/{connectionSubName}.zip";
            bool result = _compressService.CompressDirectory(connectionDirectory, zipFileName);

            if (result == true)
            {
                string readableSize = getFileHumanReadableSize(zipFileName);
                // send message
                response.payload.Clear();
                response.payload.Add("CompressStatus", $"compressed ok, file sieze: {readableSize}.");
                await _downloadHubContext.Clients.Client(connectionID).Response(response); ;
            }
            else
            {
                // send message
                response.payload.Clear();
                response.payload.Add("CompressStatus", $"compressed failed.");
                await _downloadHubContext.Clients.Client(connectionID).Response(response);
            }

            // delete directory
            Directory.Delete(connectionDirectory, true);

            return connectionSubName;
        }

        private string getFileHumanReadableSize(string filename)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = new FileInfo(filename).Length;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            // Adjust the format string to your preferences. For example "{0:0.#}{1}" would
            // show a single decimal place, and no space.
            return String.Format("{0:0.##} {1}", len, sizes[order]);
        }
    }
}
