using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PackageDownloader.Service.Interface;
using static PackageDownloader.Server.Startup;

namespace PackageDownloader.Server.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class NpmController : ControllerBase
    {
        private IPackageService _packageService;
        private readonly ILogger<NpmController> _logger;

        public NpmController(ServiceResolver serviceAccessor, ILogger<NpmController> logger)
        {
            _packageService = serviceAccessor("NpmService");
            _logger = logger;
        }

        [HttpGet]
        public async Task<IEnumerable<string>> SearchPackage(string searchName, string repostoryUrl, bool includePrerelease = false)
        {
            IEnumerable<string> packageList = await _packageService.SearchPackageAsync(searchName, repostoryUrl, includePrerelease);
            return packageList;
        }

        [HttpGet]
        public async Task<IEnumerable<string>> GetPackageVersion(string packageId, string repostoryUrl, bool includePrerelease = false)
        {
            IEnumerable<string> packageList = await _packageService.GetPackageVersionAsync(packageId, repostoryUrl, includePrerelease);
            return packageList;
        }
    }
}
