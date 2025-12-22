using System;
using System.IO;
using AlloyMVC.Extensions;
using EPiServer.Cms.Shell;
using EPiServer.Cms.UI.AspNetIdentity;
using EPiServer.Scheduler;
using EPiServer.Web.Routing;
using Krlbr.Optimizely.ImageSharp.Web;
using Krlbr.Optimizely.ImageSharp.Web.Providers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SixLabors.ImageSharp.Web.Caching.Azure;
using SixLabors.ImageSharp.Web.DependencyInjection;

namespace AlloyMVC;

public class Startup
{
    private readonly IWebHostEnvironment _webHostingEnvironment;
    private readonly IConfiguration _configuration;

    public Startup(
        IWebHostEnvironment webHostingEnvironment,
        IConfiguration configuration)
    {
        _webHostingEnvironment = webHostingEnvironment;
        _configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        if (_webHostingEnvironment.IsDevelopment())
        {
            AppDomain.CurrentDomain.SetData("DataDirectory", Path.Combine(_webHostingEnvironment.ContentRootPath, "App_Data"));

            services.Configure<SchedulerOptions>(options => options.Enabled = false);
        }

        services
            .AddCmsAspNetIdentity<ApplicationUser>()
            .AddCms()
            .AddAlloy()
            .AddAdminUserRegistration()
            .AddEmbeddedLocalization<Startup>();

        var azureBlobsConnectionString = _configuration.GetConnectionString("EPiServerAzureBlobs");

        if (!string.IsNullOrWhiteSpace(azureBlobsConnectionString))
        {
            services
                .AddImageSharp()
                .Configure<AzureBlobStorageCacheOptions>(options =>
                {
                    options.ConnectionString = azureBlobsConnectionString!;
                    options.ContainerName = "mysitemedia";
                })
                .ClearProviders()
                .AddProvider<BlobImageProvider>()
                .SetCache<AzureBlobStorageCache>();
            services.AddMinimalCmsCloudPlatformSupport(_configuration);
        }
        else
        {
            services.AddKrlbrOptimizelyImageSharp();
        }
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseKrlbrOptimizelyImageSharp();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapContent();
        });
    }
}