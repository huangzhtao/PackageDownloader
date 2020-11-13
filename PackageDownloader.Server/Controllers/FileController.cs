using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace PackageDownloader.Server.Controllers
{
    [Route("download/[controller]")]
    public class FileController : ControllerBase
    {
        [HttpGet]
        public FileResult GetFileDownload(string name)
        {
            string filePath = $"/packages/{name}.zip";
            //if (System.IO.File.Exists(filePath))
            //{
                return File(filePath, "application/zip", $"{name}.zip");
            //}

        }
    }
}
