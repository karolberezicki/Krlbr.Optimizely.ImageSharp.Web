using Krlbr.Optimizely.ImageSharp.Web.Caching;
using Krlbr.Optimizely.ImageSharp.Web.Providers;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using SixLabors.ImageSharp.Web.DependencyInjection;
using SixLabors.ImageSharp.Web.Providers;

namespace Krlbr.Optimizely.ImageSharp.Web
{
    public static class ServiceAndAppExtensions
    {
        public static void AddKrlbrOptimizelyImageSharp(this IServiceCollection services)
        {
            services.AddImageSharp()
                    .ClearProviders()
                    .AddProvider<BlobImageProvider>()
                    .AddProvider<PhysicalFileSystemProvider>()
                    .SetCache<BlobImageCache>();
        }

        public static void UseKrlbrOptimizelyImageSharp(this IApplicationBuilder app)
        {
            app.UseImageSharp();
        }
    }
}
