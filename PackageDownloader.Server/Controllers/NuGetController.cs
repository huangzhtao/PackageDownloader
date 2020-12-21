using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PackageDownloader.NuGet;
using PackageDownloader.Service.Interface;
using PackageDownloader.Shared.NuGet;
using static PackageDownloader.Server.Startup;

namespace PackageDownloader.Server.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class NuGetController : ControllerBase
    {
        private IPackageService _packageService;
        private readonly ILogger<NuGetController> _logger;

        public NuGetController(ServiceResolver serviceAccessor, ILogger<NuGetController> logger)
        {
            _packageService = serviceAccessor("NuGetService");
            _logger = logger;
        }

        [HttpGet]
        public async Task<IEnumerable<string>> SearchPackage(string searchName, 
            string repostoryUrl = "https://api.nuget.org/v3/index.json", bool includePrerelease = false)
        {
            IEnumerable<string> packageList = await _packageService.SearchPackageAsync(searchName, repostoryUrl, includePrerelease);
            return packageList;
        }

        [HttpGet]
        public async Task<IEnumerable<string>> GetPackageVersion(string packageId,
            string repostoryUrl = "https://api.nuget.org/v3/index.json", bool includePrerelease = false)
        {
            IEnumerable<string> packageList = await _packageService.GetPackageVersionAsync(packageId, repostoryUrl, includePrerelease);
            return packageList;
        }
    }
}
