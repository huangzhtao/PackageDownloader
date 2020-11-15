using PackageDownloader.Server.Services.Npm.Query;
using System;
using System.Collections.Generic;
using System.Text;

namespace PackageDownloader.Server.Services.Npm.Query
{
    public class QueryResult
    {
        public IEnumerable<NpmPackage> objects { get; set; }
        public int total { get; set; }
        public string time { get; set; }
    }
}
