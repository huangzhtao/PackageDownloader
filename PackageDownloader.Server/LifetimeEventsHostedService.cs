using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PackageDownloader.Server
{
    public class LifetimeEventsHostedService : IHostedService
    {
        private readonly ILogger _logger;
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly IConfiguration _configuration;

        private DateTime _startTime;
        private string _runInstance;

        public LifetimeEventsHostedService(ILogger<LifetimeEventsHostedService> logger,
            IHostApplicationLifetime appLifetime, IConfiguration configuration)
        {
            _logger = logger;
            _appLifetime = appLifetime;
            _configuration = configuration;

            _runInstance = Guid.NewGuid().ToString();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _appLifetime.ApplicationStarted.Register(OnStarted);
            _appLifetime.ApplicationStopped.Register(OnStopped);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private void OnStarted()
        {
            _startTime = DateTime.Now;

            // get api address
            RestClient client = new RestClient();
            string apiUrl = _configuration.GetValue<string>("IP-API-URL");
            var requestGet = new RestRequest(apiUrl, Method.GET);
            IRestResponse result = client.Execute(requestGet);

            _logger.LogInformation($"start instance: {_runInstance}, {result.Content}.");
        }

        private void OnStopped()
        {
            TimeSpan ts = DateTime.Now - _startTime;
            _logger.LogInformation($"stop instance: {_runInstance}, {ts.ToString("0:dd\\.hh\\:mm\\:ss")} days.");
        }
    }
}
