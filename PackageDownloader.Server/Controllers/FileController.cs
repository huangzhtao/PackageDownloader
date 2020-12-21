using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace PackageDownloader.Server.Controllers
{
    [Route("download/[controller]")]
    public class FileController : ControllerBase
    {
        private readonly ILogger<FileController> _logger;

        public FileController(ILogger<FileController> logger) => _logger = logger;

        [HttpGet]
        public FileResult GetFileDownload(string name)
        {
            string filePath = $"/packages/{name}.zip";
            return File(filePath, "application/zip", $"{name}.zip");
        }
    }
}
