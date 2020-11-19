using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using PackageDownloader.Server.Hubs;
using PackageDownloader.Server.Utils;
using PackageDownloader.Service.Interface;
using PackageDownloader.Shared;
using PackageDownloader.Shared.Container;
using PackageDownloader.Shared.Response;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PackageDownloader.Server.Services.Docker
{
    public class ContainerService : IPackageService
    {
        private readonly IHubContext<DownloadPackageHub, IDownloadPackageHubClient> _downloadHubContext;
        private readonly ICompressService _compressService;
        private readonly IHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        private class PackageInfo
        {
            public string packageId { get; set; }
            public SemVer.Version packageVersion { get; set; }
            public int depth { get; set; }
        }

        RestClient client = new RestClient();

        // Configure
        int MessageFrequency = 10; // default freq every 10 download message send to client 
        private readonly string DefaultDockerRegistry;
        private readonly int SearchSize;

        public ContainerService(IHubContext<DownloadPackageHub, IDownloadPackageHubClient> hubContext, ICompressService compressService, IHostEnvironment environment, IConfiguration configuration)
        {
            _downloadHubContext = hubContext;
            _compressService = compressService;
            _environment = environment;
            _configuration = configuration;

            // get configure value
            MessageFrequency = _configuration.GetValue<int>("MessageFrequency");
            DefaultDockerRegistry = _configuration.GetSection("Docker").GetValue<string>("DefaultRepository");
            SearchSize = _configuration.GetSection("Docker").GetValue<int>("SearchSize");
        }

        public Task<IEnumerable<string>> SearchPackageAsync(string searchPackageName, string repositoryUrl, bool includePrerelease)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<string>> GetPackageVersionAsync(string packageId, string repositoryUrl, bool includePrerelease)
        {
            throw new NotImplementedException();
        }

        public async Task<string> DownloadPackageAsync(string connectionID, RequestDownloadInfo requestInfo)
        {
            // cast
            RequestDownloadContainerInfo info = (RequestDownloadContainerInfo)requestInfo;

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

            string connectionSubName = $"docker-{connectionID}-{DateTime.Now:yyyymmddHHmmss}";

            // send message
            response.payload.Clear();
            response.payload.Add("Resource", $"{connectionSubName} created.");
            await _downloadHubContext.Clients.Client(connectionID).Response(response);

            // docker pull
            Process compiler = new Process();
            compiler.StartInfo.FileName = "docker";
            compiler.StartInfo.Arguments = $"pull {info.image}";
            compiler.StartInfo.UseShellExecute = false;
            compiler.StartInfo.RedirectStandardOutput = true;
            compiler.OutputDataReceived += async (sender, args) => await sendMessageAsync(connectionID, args.Data);
            compiler.Start();
            compiler.BeginOutputReadLine();
            compiler.WaitForExit();

            // send message
            response.payload.Clear();
            response.payload.Add("DownloadCounter", $"download finished.");
            await _downloadHubContext.Clients.Client(connectionID).Response(response);

            // docker save
            Process compiler2 = new Process();
            compiler2.StartInfo.FileName = "docker";
            compiler2.StartInfo.Arguments = $"save -o {connectionSubName}.zip {info.image}";
            compiler2.StartInfo.UseShellExecute = false;
            compiler2.StartInfo.RedirectStandardOutput = true;
            compiler2.OutputDataReceived += (sender, args) => Console.WriteLine("received output: {0}", args.Data);
            compiler2.Start();
            compiler2.BeginOutputReadLine();
            compiler2.WaitForExit();

            string readableSize = FileUtil.getFileHumanReadableSize($"{connectionSubName}.zip");
            // send message
            response.payload.Clear();
            response.payload.Add("CompressStatus", $"compressed ok, file sieze: {readableSize}.");
            await _downloadHubContext.Clients.Client(connectionID).Response(response);

            // delete image
            Process compiler3 = new Process();
            compiler3.StartInfo.FileName = "docker";
            compiler3.StartInfo.Arguments = $"rmi {info.image}";
            compiler3.StartInfo.UseShellExecute = false;
            compiler3.Start();
            compiler3.WaitForExit();

            return connectionSubName;
        }

        private async Task sendMessageAsync(string connectionID, string message)
        {
            if (message != null && message != "")
            {
                // server response
                ServerResponse response = new ServerResponse()
                {
                    payload = new Dictionary<string, string>()
                };

                // send message
                response.payload.Clear();
                response.payload.Add("DownloadCounter", message);
                await _downloadHubContext.Clients.Client(connectionID).Response(response);
            }
        }
    }
}
