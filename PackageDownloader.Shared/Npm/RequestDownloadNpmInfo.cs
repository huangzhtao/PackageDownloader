using System;
using System.Collections.Generic;
using System.Text;

namespace PackageDownloader.Shared.Npm
{
    public class RequestDownloadNpmInfo : RequestDownloadInfo
    {
        public IEnumerable<NpmPackageInfo> packageList { get; set; }
        public bool preReleased { get; set; }
        public bool withDependency { get; set; }
        public bool withDevDependency { get; set; }
        public int dependencyDepth { get; set; }
        public string repository { get; set; }
    }
}
