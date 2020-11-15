using System;
using System.Collections.Generic;
using System.Text;

namespace PackageDownloader.Server.Services.Npm.Package
{
    public class PackageDist
    {
        public string shasum { get; set; }
        public string tarball { get; set; }
    }
}
