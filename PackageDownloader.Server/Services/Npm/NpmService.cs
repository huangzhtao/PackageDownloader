using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PackageDownloader.Server.Hubs;
using PackageDownloader.Server.Services.Npm.Package;
using PackageDownloader.Server.Services.Npm.Query;
using PackageDownloader.Server.Utils;
using PackageDownloader.Service.Interface;
using PackageDownloader.Shared;
using PackageDownloader.Shared.Npm;
using PackageDownloader.Shared.Response;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PackageDownloader.Server.Services.Npm
{
    public class NpmService : IPackageService
    {
        private readonly IHubContext<DownloadPackageHub, IDownloadPackageHubClient> _downloadHubContext;
        private readonly ICompressService _compressService;
        private readonly IHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly ILogger<NpmService> _logger;

        private class PackageInfo
        {
            public string packageId { get; set; }
            public SemVer.Version packageVersion { get; set; }
            public int depth { get; set; }
        }

        Queue<PackageInfo> _downloadQueue = new Queue<PackageInfo>();
        List<string> _cacheDownloadedFileName = new List<string>();
        HashSet<string> _cacheForPackageVersion = new HashSet<string>();
        private Dictionary<string, IEnumerable<string>> _cacheForVersions = new Dictionary<string, IEnumerable<string>>();

        RestClient client = new RestClient();

        // Configure
        int MessageFrequency = 10; // default freq every 10 download message send to client 
        private readonly string DefaultNpmRegistry;
        private readonly int SearchSize;

        public NpmService(IHubContext<DownloadPackageHub, IDownloadPackageHubClient> hubContext, ICompressService compressService, IHostEnvironment environment, IConfiguration configuration, ILogger<NpmService> logger)
        {
            _downloadHubContext = hubContext;
            _compressService = compressService;
            _environment = environment;
            _configuration = configuration;
            _logger = logger;

            // get configure value
            MessageFrequency = _configuration.GetValue<int>("MessageFrequency");
            DefaultNpmRegistry = _configuration.GetSection("NPM").GetValue<string>("DefaultRepository");
            SearchSize = _configuration.GetSection("NPM").GetValue<int>("SearchSize");
        }

        public async Task<IEnumerable<string>> SearchPackageAsync(string searchPackageName, string repositoryUrl, bool includePrerelease)
        {
            // base url
            string baseUrl = (repositoryUrl == null || repositoryUrl == "") ? DefaultNpmRegistry : repositoryUrl;
            client.BaseUrl = new Uri(baseUrl);

            // search url
            string searchUrl = $"/-/v1/search?text={searchPackageName}&size={SearchSize}";

            var requestGet = new RestRequest(searchUrl, Method.GET);
            IRestResponse<QueryResult> result = await client.ExecuteAsync<QueryResult>(requestGet);
            return result.Data?.objects?.Select(x => x.package.name);
        }

        public async Task<IEnumerable<string>> GetPackageVersionAsync(string packageId, string repositoryUrl, bool includePrerelease)
        {
            // get from cache
            if (_cacheForVersions.ContainsKey(packageId))
            {
                return _cacheForVersions[packageId];
            }

            // base url
            string baseUrl = (repositoryUrl == null || repositoryUrl == "") ? DefaultNpmRegistry : repositoryUrl;
            client.BaseUrl = new Uri(baseUrl);

            // search url
            string searchUrl = $"/{packageId}";

            var requestGet = new RestRequest(searchUrl, Method.GET);
            IRestResponse<PackageAttribute> result = await client.ExecuteAsync<PackageAttribute>(requestGet);

            IEnumerable<string> versionList = new List<string>();
            if (includePrerelease == true)
            {
                versionList = result.Data?.versions?.Values.ToList().Select(x => new SemVer.Version(x.version, true)).
                    AsEnumerable().OrderByDescending(x => x).Select(x => x.ToString());
            }
            else
            {
                versionList = result.Data?.versions?.Values.ToList().Select(x => new SemVer.Version(x.version, true)).
                    AsEnumerable().Where(x => x.PreRelease is null).OrderByDescending(x => x).Select(x => x.ToString());
            }

            // add cache
            _cacheForVersions.Add(packageId, versionList);
            return versionList;
        }

        private async Task<PackageInfo> GetBestMatchPackageVersionsAsync(string registry, bool includePrerelease, string packageId, SemVer.Range range)
        {
            PackageOption option = new PackageOption
            {
                packageId = packageId,
                includePrerelease = includePrerelease,
                registry = registry
            };

            Console.WriteLine($"Try to find best match: {packageId}: {range.ToString()}");

            IEnumerable<string> versionValues = await GetPackageVersionAsync(option.packageId, option.registry, option.includePrerelease);

            IEnumerable<SemVer.Version> versions = versionValues?.Select(x => new SemVer.Version(x, true));

            if (versions == null)
            {
                return null;
            }

            SemVer.Version bestMatchVersion = range.MaxSatisfying(versions);

            if (bestMatchVersion == null)
            {
                bestMatchVersion = versions.OrderByDescending(x => x).FirstOrDefault();
            }

            return new PackageInfo { packageId = packageId, packageVersion = bestMatchVersion };
        }

        private async Task<PackageVersion> GetPackageVersionAsync(string registry, string packageId, SemVer.Version version)
        {
            // check version is null
            if (version == null)
            {
                // get max version of package
                PackageInfo package = await GetBestMatchPackageVersionsAsync(registry, true, packageId, new SemVer.Range("*"));
                version = package.packageVersion;
            }

            // base url
            string baseUrl = (registry == null || registry == "") ? DefaultNpmRegistry : registry;
            client.BaseUrl = new Uri(baseUrl);

            // get version url
            if (packageId == null || version == null)
            {
                return null;
            }
            else
            {
                string searchUrl = $"/{packageId}/{version.ToString()}";

                var requestGet = new RestRequest(searchUrl, Method.GET);
                IRestResponse<PackageVersion> result = await client.ExecuteAsync<PackageVersion>(requestGet);
                return result.Data;
            }
        }

        private async Task<PackageInfo> GetPackageInfoByStringAsync(string packageId, string packageVersionValue,
                                                                            bool preReleased, string repository)
        {
            PackageInfo package = null;
            // if packageVersionValue is null, get the newest version
            if (packageVersionValue == null || packageVersionValue == "")
            {
                PackageOption option = new PackageOption()
                {
                    packageId = packageId,
                    includePrerelease = preReleased,
                    registry = repository
                };
                IEnumerable<string> versionList = await GetPackageVersionAsync(option.packageId, option.registry, option.includePrerelease);

                if (versionList != null)
                {
                    packageVersionValue = versionList.ElementAt(0);
                }
                else
                {
                    return null;
                }
            }
            SemVer.Version packageVersion = null;
            SemVer.Range range = null;
            if (SemVer.Version.TryParse(packageVersionValue, out packageVersion) == true)
            {
                // version
                Console.WriteLine($"Convet to version: {packageId}: {packageVersionValue}");
                package = new PackageInfo { packageId = packageId, packageVersion = packageVersion };
            }
            else if (SemVer.Range.TryParse(packageVersionValue, out range))
            {
                // range
                Console.WriteLine($"Convet to range: {packageId}: {packageVersionValue}");
                package = await GetBestMatchPackageVersionsAsync(repository, preReleased, packageId, range);
                Console.WriteLine($"Find best match: {packageId}: {package?.packageVersion?.ToString()}");
            }
            else
            {
                // range of *
                Console.WriteLine($"Convet to range: {packageId}: {packageVersionValue}");
                range = new SemVer.Range("*");
                package = await GetBestMatchPackageVersionsAsync(repository, preReleased, packageId, range);
                Console.WriteLine($"Find best match: {packageId}: {package?.packageVersion?.ToString()}");
            }
            return package;
        }

        public async Task<string> DownloadPackageAsync(string connectionID, RequestDownloadInfo requestInfo)
        {
            // cast
            RequestDownloadNpmInfo info = (RequestDownloadNpmInfo)requestInfo;

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

            string connectionSubName = $"npm-{connectionID}-{DateTime.Now:yyyymmddHHmmss}";
            string connectionDirectory = $"{_outputDirectory}/{connectionSubName}";
            Directory.CreateDirectory(connectionDirectory);

            // send message
            response.payload.Clear();
            response.payload.Add("Resource", $"{connectionSubName} created.");
            await _downloadHubContext.Clients.Client(connectionID).Response(response);

            // set all parent packages
            for (int i = 0; i < info.packageList.Count(); i++)
            {
                string packageId = info.packageList.ElementAt(i).packageId;
                string packageVersionValue = info.packageList.ElementAt(i).packageVersion;
                string setValue = $"{packageId}-{packageVersionValue}";

                PackageInfo packageInfo = await GetPackageInfoByStringAsync(packageId, packageVersionValue,
                                                                              info.preReleased, info.repository);
                if (packageInfo != null)
                {
                    packageInfo.depth = 0;
                    _downloadQueue.Enqueue(packageInfo);
                    _cacheForPackageVersion.Add(setValue);
                }
            }

            // download counter
            int download_counter = 0;
            while (_downloadQueue.Count > 0)
            {
                PackageInfo package = _downloadQueue.Dequeue();

                string validFileName = FileUtil.GetValidFileName(package.packageId);
                string packageFilePath = $"{connectionDirectory}/{validFileName}-{package.packageVersion}.tgz";

                if (_cacheDownloadedFileName.Contains($"{package.packageId}-{package.packageVersion}"))
                {
                    continue;
                }
                else
                {
                    _cacheDownloadedFileName.Add($"{package.packageId}-{package.packageVersion}");
                }

                // Get specific package and version
                PackageVersion packageVerion = await GetPackageVersionAsync(info.repository, package.packageId, package.packageVersion);

                // Downlaod package
                if (packageVerion != null && packageVerion.dist != null && packageVerion.dist.tarball != null)
                {
                    using var packageStream = new FileStream(packageFilePath, FileMode.Create);
                    using var httpStream = await new HttpClient().GetStreamAsync(packageVerion.dist.tarball);
                    await httpStream.CopyToAsync(packageStream);
                }
                else
                {
                    Console.WriteLine($"Error: tarball url is null, {package.packageId}");
                    continue;
                }

                download_counter++;

                // starting
                if (download_counter == 1)
                {
                    // send message
                    response.payload.Clear();
                    response.payload.Add("DownloadCounter", $"starting...");
                    await _downloadHubContext.Clients.Client(connectionID).Response(response);
                }

                // check if send message is needed
                if (download_counter % MessageFrequency == 0)
                {
                    Console.WriteLine($"DownloadCounter: {download_counter}, download queue: {_downloadQueue.Count}," +
                        $" file: {_cacheDownloadedFileName.Count}, parse: {_cacheForPackageVersion.Count}," +
                        $" versions: {_cacheForVersions.Count}, percent: {((float)download_counter / (float)(download_counter + _downloadQueue.Count)) * 100.0f} %");

                    // send message
                    response.payload.Clear();
                    response.payload.Add("DownloadCounter", $"{download_counter}, {((float)download_counter / (float)(download_counter + _downloadQueue.Count)) * 100.0f}%");
                    await _downloadHubContext.Clients.Client(connectionID).Response(response);
                }

                Console.WriteLine($"Downloaded package {package.packageId} {package.packageVersion}");
                //Console.WriteLine($"Author: {packageVerion.author}");
                Console.WriteLine($"Description: {packageVerion.description}");

                if (info.withDependency == false)
                {
                    Console.WriteLine("\nDependencies download is not need.");
                    continue;
                }

                Console.WriteLine("\nStart download dependencies:");
                if (packageVerion.dependencies != null && (info.dependencyDepth == -1 || package.depth < info.dependencyDepth))
                {
                    foreach (var dependencyGroup in packageVerion.dependencies)
                    {
                        string packageId = dependencyGroup.Key;
                        string packageVersionValue = dependencyGroup.Value;
                        string setValue = $"{packageId}-{packageVersionValue}";

                        if (_cacheForPackageVersion.Contains(setValue) == true)
                        {
                            Console.WriteLine($"Already parsed: {setValue}");
                            continue;
                        }

                        PackageInfo packageInfo = await GetPackageInfoByStringAsync(packageId, packageVersionValue,
                                                                                      info.preReleased, info.repository);
                        if (packageInfo != null)
                        {
                            packageInfo.depth = package.depth + 1;
                            if (info.dependencyDepth == -1 || packageInfo.depth <= info.dependencyDepth)
                            {
                                _downloadQueue.Enqueue(packageInfo);
                                _cacheForPackageVersion.Add(setValue);
                            }
                        }
                    }
                }

                if (info.withDevDependency == false)
                {
                    Console.WriteLine("\nDev dependencies download is not need.");
                    continue;
                }

                Console.WriteLine("\nStart download dev dependencies:");
                if (packageVerion.devDependencies != null && (info.dependencyDepth == -1 || package.depth < info.dependencyDepth))
                {
                    foreach (var dependencyGroup in packageVerion.devDependencies)
                    {
                        string packageId = dependencyGroup.Key;
                        string packageVersionValue = dependencyGroup.Value;
                        string setValue = $"{packageId}-{packageVersionValue}";

                        if (_cacheForPackageVersion.Contains(setValue) == true)
                        {
                            Console.WriteLine($"Already parsed: {setValue}");
                            continue;
                        }

                        PackageInfo packageInfo = await GetPackageInfoByStringAsync(packageId, packageVersionValue,
                                                                                      info.preReleased, info.repository);
                        if (packageInfo != null)
                        {
                            packageInfo.depth = package.depth + 1;
                            if (info.dependencyDepth == -1 || packageInfo.depth <= info.dependencyDepth)
                            {
                                _downloadQueue.Enqueue(packageInfo);
                                _cacheForPackageVersion.Add(setValue);
                            }
                        }
                    }
                }
            }

            Console.WriteLine($"DownloadCounter: {download_counter}, download queue: {_downloadQueue.Count}," +
                        $" file: {_cacheDownloadedFileName.Count}, parse: {_cacheForPackageVersion.Count}," +
                        $" versions: {_cacheForVersions.Count}, percent: {((float)download_counter / (float)(download_counter + _downloadQueue.Count)) * 100.0f} %");

            // send message
            response.payload.Clear();
            response.payload.Add("DownloadCounter", $"{download_counter}, {((float)download_counter / (float)(download_counter + _downloadQueue.Count)) * 100.0f}%");
            await _downloadHubContext.Clients.Client(connectionID).Response(response);

            string zipFileName = $"{_outputDirectory}/{connectionSubName}.zip";
            bool result = _compressService.CompressDirectory(connectionDirectory, zipFileName);

            if (result == true)
            {
                string readableSize = FileUtil.getFileHumanReadableSize(zipFileName);
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

    }
}
