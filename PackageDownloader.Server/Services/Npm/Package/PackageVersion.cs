using System;
using System.Collections.Generic;
using System.Text;

namespace PackageDownloader.Server.Services.Npm.Package
{
    public class PackageVersion
    {
        public string name { get; set; }
        public string version { get; set; }
        public string description { get; set; }
        public IEnumerable<string> keywords { get; set; }
        public PackageDist dist { get; set; }
        public Dictionary<string, string> dependencies { get; set; }
        public Dictionary<string, string> devDependencies { get; set; }
    }
}
