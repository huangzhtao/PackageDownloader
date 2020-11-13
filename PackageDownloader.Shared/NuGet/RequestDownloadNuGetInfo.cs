using System;
using System.Collections.Generic;
using System.Text;

namespace PackageDownloader.Shared.NuGet
{
    public class RequestDownloadNuGetInfo: RequestDownloadInfo
    {
        public IEnumerable<NuGetPackageInfo> packageList { get; set; }
        public bool preReleased { get; set; }
        public bool withDependency { get; set; }
        public IEnumerable<string> targetFramework { get; set; }
        public string repository { get; set; }
    }
}
