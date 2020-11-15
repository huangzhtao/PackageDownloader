using System;
using System.Collections.Generic;
using System.Text;

namespace PackageDownloader.Server.Services.Npm.Query
{
    public class PackageOption
    {
        public string packageId { get; set; }
        public bool includePrerelease { get; set; }
        public string registry { get; set; }
    }
}
