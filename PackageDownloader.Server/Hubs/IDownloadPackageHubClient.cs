using PackageDownloader.Shared.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PackageDownloader.Server.Hubs
{
    public interface IDownloadPackageHubClient
    {
        Task Message(string message);
        Task Response(ServerResponse response);
    }
}
