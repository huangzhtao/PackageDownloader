using System;
using System.Collections.Generic;
using System.Text;

namespace PackageDownloader.Server.Services.Npm.Package
{
    public class PackageAttribute
    {
        public string name { get; set; }
        public string description { get; set; }
        public Dictionary<string, string> distTags { get; set; }
        public Dictionary<string, PackageVersion> versions { get; set; }
    }
}
