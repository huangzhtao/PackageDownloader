using PackageDownloader.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PackageDownloader.Service.Interface
{
    public interface IPackageService
    {
        Task<IEnumerable<string>> SearchPackageAsync(string searchPackageName, string repositoryUrl, bool includePrerelease);
        Task<IEnumerable<string>> GetPackageVersionAsync(string packageId, string repositoryUrl, bool includePrerelease);
        Task<string> DownloadPackageAsync(string connectionID, RequestDownloadInfo info);
    }
}
