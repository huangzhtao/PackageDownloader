using System;
using System.Collections.Generic;
using System.Text;

namespace PackageDownloader.Shared.User
{
    public class ConnectionInfo
    {
        public string connectionID { get; set; }
        public string userIP { get; set; }
        public DateTime connectionTime { get; set; }
    }
}
