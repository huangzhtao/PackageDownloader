using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PackageDownloader.NuGet;
using PackageDownloader.Server.Hubs;
using PackageDownloader.Server.Services.Container;
using PackageDownloader.Server.Services.Npm;
using PackageDownloader.Service.Compress;
using PackageDownloader.Service.Interface;

namespace PackageDownloader.Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public delegate IPackageService ServiceResolver(string key);

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSignalR();
            services.AddControllers();
            services.AddMemoryCache();
            services.AddResponseCompression(opts =>
            {
                opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                    new[] { "application/octet-stream" });
            });

            services.AddScoped<ICompressService, CompressService>();
            services.AddScoped<NuGetService>();
            services.AddScoped<NpmService>();

            services.AddScoped<ServiceResolver>(serviceProvider => key =>
            {
                switch (key)
                {
                    case "NuGetService":
                        return serviceProvider.GetService<NuGetService>();
                    case "NpmService":
                        return serviceProvider.GetService<NpmService>();
                    case "ContainerService":
                        return serviceProvider.GetService<ContainerService>();
                    default:
                        throw new KeyNotFoundException();
                }
            });

            services.AddSwaggerGen();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseResponseCompression();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles();
            app.UseBlazorFrameworkFiles();

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "API V1");
            });

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<DownloadPackageHub>("/DownloadPackageHub");
                endpoints.MapFallbackToFile("index.html");
            });
        }
    }
}
