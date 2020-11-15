using System;
using System.Collections.Generic;
using System.Text;

namespace PackageDownloader.Server.Services.Npm.Query
{
    public class QueryOption
    {
        public string keyword { get; set; }
        public int size { get; set; }
        public string registry { get; set; }
    }
}
