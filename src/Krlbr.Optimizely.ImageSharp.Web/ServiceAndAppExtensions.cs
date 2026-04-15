using Krlbr.Optimizely.ImageSharp.Web.Caching;
using Krlbr.Optimizely.ImageSharp.Web.Providers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using SixLabors.ImageSharp.Web.DependencyInjection;
using SixLabors.ImageSharp.Web.Providers;

namespace Krlbr.Optimizely.ImageSharp.Web;

public static class ServiceAndAppExtensions
{
    extension(IServiceCollection services)
    {
        public void AddKrlbrOptimizelyImageSharp()
        {
            services.AddImageSharp()
                .ClearProviders()
                .AddProvider<BlobImageProvider>()
                .AddProvider<PhysicalFileSystemProvider>()
                .SetCache<BlobImageCache>();
        }
    }

    extension(IApplicationBuilder app)
    {
        public void UseKrlbrOptimizelyImageSharp()
        {
            app.UseImageSharp();
        }
    }
}