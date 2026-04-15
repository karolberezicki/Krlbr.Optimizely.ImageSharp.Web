using Alloy.Extensions;
using EPiServer.Cms.UI.AspNetIdentity;
using EPiServer.Data;
using EPiServer.DependencyInjection;
using EPiServer.Scheduler;
using EPiServer.Web.Routing;
using Krlbr.Optimizely.ImageSharp.Web;
using Krlbr.Optimizely.ImageSharp.Web.Providers;
using SixLabors.ImageSharp.Web.Caching.Azure;
using SixLabors.ImageSharp.Web.DependencyInjection;

namespace Alloy;

public class Startup(IWebHostEnvironment webHostingEnvironment, IConfiguration configuration)
{
    public void ConfigureServices(IServiceCollection services)
    {
        if (webHostingEnvironment.IsDevelopment())
        {
            AppDomain.CurrentDomain.SetData("DataDirectory", Path.Combine(webHostingEnvironment.ContentRootPath, "App_Data"));

            services.Configure<SchedulerOptions>(options => options.Enabled = false);
        }

        services.Configure<DataAccessOptions>(o => o.UpdateDatabaseCompatibilityLevel = true);

        services
            .AddCmsAspNetIdentity<ApplicationUser>()
            .AddCms()
            .AddAlloy()
            .AddAdminUserRegistration()
            .AddEmbeddedLocalization<Startup>();

        // Required by Wangkanai.Detection
        services.AddDetection();

        services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromSeconds(10);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
        });

        var azureBlobsConnectionString = configuration.GetConnectionString("EPiServerAzureBlobs");

        if (!string.IsNullOrWhiteSpace(azureBlobsConnectionString))
        {
            services
                .AddImageSharp()
                .Configure<AzureBlobStorageCacheOptions>(options =>
                {
                    options.ConnectionString = azureBlobsConnectionString!;
                    options.ContainerName = "mysitemedia";
                    options.CacheFolder = "_is_cache";
                })
                .ClearProviders()
                .AddProvider<BlobImageProvider>()
                .SetCache<AzureBlobStorageCache>();
            services.AddMinimalCmsCloudPlatformSupport(configuration);
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

        // Required by Wangkanai.Detection
        app.UseDetection();
        app.UseSession();

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
