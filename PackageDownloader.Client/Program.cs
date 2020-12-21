using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Blazorise;
using Blazorise.Bootstrap;
using Blazorise.Icons.FontAwesome;
using Sentry;

namespace PackageDownloader.Client
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Capture blazor bootstrapping errors
            using var sdk = SentrySdk.Init(o =>
            {
                o.Dsn = "https://9f9094c5f4684ba0a9b3789c2cfec972@o494154.ingest.sentry.io/5564517";
                o.Debug = true;
            });
            try
            {
                var builder = WebAssemblyHostBuilder.CreateDefault(args);

                builder.RootComponents.Add<App>("#app");

                builder.Services.AddScoped(sp => new HttpClient
                { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

                builder.Services
                    .AddBlazorise(options =>
                    {
                        options.ChangeTextOnKeyPress = true;
                    })
                    .AddBootstrapProviders()
                    .AddFontAwesomeIcons();

                var host = builder.Build();

                host.Services
                  .UseBootstrapProviders()
                  .UseFontAwesomeIcons();

                await host.RunAsync();
            }
            catch (Exception e)
            {
                SentrySdk.CaptureException(e);
                await SentrySdk.FlushAsync(TimeSpan.FromSeconds(2));
                throw;
            }
        }
    }
}
