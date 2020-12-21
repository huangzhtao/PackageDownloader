using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PackageDownloader.Server.Controllers
{
    [Route("manage/[controller]")]
    public class ManageController
    {
        private readonly IHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ManageController> _logger;

        public ManageController(IHostEnvironment environment, IConfiguration configuration, ILogger<ManageController> logger)
        {
            _environment = environment;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        public List<string> ShowDownloadedFile(string key)
        {
            if (key != DateTime.Now.ToString("yyyy-MM-dd"))
            {
                return null;
            }

            string _outputDirectory = $"{_environment.ContentRootPath}/wwwroot/{_configuration.GetValue<string>("DownloadPath")}";

            List<string> showFiles = new List<string>();
            DirectoryInfo dir = new DirectoryInfo(_outputDirectory);
            FileSystemInfo[] fileinfo = dir.GetFileSystemInfos();
            foreach (FileSystemInfo i in fileinfo)
            {
                if (i is DirectoryInfo)
                {
                    showFiles.Add($"{i.Name}/");
                }
                else
                {
                    showFiles.Add(i.Name);
                }
            }
            return showFiles;
        }

        [HttpPost]
        public List<string> DeleteDownloadedFile(string key)
        {
            if (key != DateTime.Now.ToString("yyyy-MM-dd"))
            {
                return null;
            }

            string _outputDirectory = $"{_environment.ContentRootPath}/wwwroot/{_configuration.GetValue<string>("DownloadPath")}";

            List<string> deletedFiles = new List<string>();
            DirectoryInfo dir = new DirectoryInfo(_outputDirectory);
            FileSystemInfo[] fileinfo = dir.GetFileSystemInfos();
            foreach (FileSystemInfo i in fileinfo)
            {
                if (i is DirectoryInfo)
                {
                    DirectoryInfo subdir = new DirectoryInfo(i.FullName);
                    subdir.Delete(true);
                    deletedFiles.Add($"{i.Name}/");
                }
                else
                {
                    File.Delete(i.FullName);
                    deletedFiles.Add(i.Name);
                }
            }
            return deletedFiles;
        }
    }
}
