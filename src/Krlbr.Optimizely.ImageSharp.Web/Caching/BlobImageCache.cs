using System;
using System.IO;
using System.Threading.Tasks;
using EPiServer.Core;
using EPiServer.Framework.Blobs;
using EPiServer.ServiceLocation;
using EPiServer.Web.Routing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp.Web;
using SixLabors.ImageSharp.Web.Caching;
using SixLabors.ImageSharp.Web.Middleware;
using SixLabors.ImageSharp.Web.Resolvers;

namespace Krlbr.Optimizely.ImageSharp.Web.Caching;

/// <summary>
/// Implements an Optimizely blob based cache.
/// </summary>
public class BlobImageCache : IImageCache
{
    /// <summary>
    /// The root path for the cache.
    /// </summary>
    private readonly string _cacheRootPath;

    /// <summary>
    /// The length of the filename to use (minus the extension) when storing images in the image cache.
    /// </summary>
    private readonly int _cacheHashLength;

    /// <summary>
    /// The cache configuration options.
    /// </summary>
    private readonly BlobImageCacheOptions _cacheOptions;

    /// <summary>
    /// The middleware configuration options.
    /// </summary>
    private readonly ImageSharpMiddlewareOptions _options;

    /// <summary>
    /// Contains various format helper methods based on the current configuration.
    /// </summary>
    private readonly FormatUtilities _formatUtilities;

    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlobImageCache"/> class.
    /// </summary>
    /// <param name="cacheOptions">The cache configuration options.</param>
    /// <param name="environment">The hosting environment the application is running in.</param>
    /// <param name="options">The middleware configuration options.</param>
    /// <param name="formatUtilities">Contains various format helper methods based on the current configuration.</param>
    /// <param name="httpContextAccessor">The HTTP context accessor.</param>
    public BlobImageCache(IOptions<BlobImageCacheOptions>? cacheOptions, IWebHostEnvironment environment, IOptions<ImageSharpMiddlewareOptions> options, FormatUtilities formatUtilities, IHttpContextAccessor httpContextAccessor)
    {
        // Allow configuration of the cache without having to register everything.
        _cacheOptions = cacheOptions is not null ? cacheOptions.Value : new();

        _httpContextAccessor = httpContextAccessor;

        _cacheRootPath = Path.GetFullPath(Path.Combine(environment.ContentRootPath, _cacheOptions.CacheFolder));
        //fileProvider = new PhysicalFileProvider(cacheRootPath);
        _options = options.Value;
        _cacheHashLength = (int)_options.CacheHashLength;
        _formatUtilities = formatUtilities;
    }

    /// <inheritdoc/>
    public async Task<IImageCacheResolver?> GetAsync(string key)
    {
        var container = GetContainer();
        var fileId = $"{Blob.BlobUriScheme}://{Blob.DefaultProvider}/{container}/{_cacheOptions.Prefix}{key}";

        var blobFactory = ServiceLocator.Current.GetInstance<IBlobFactory>();
        Uri uri = new($"{fileId}.meta");

        var blob = blobFactory.GetBlob(uri);

        var metaFileInfo = await blob.AsFileInfoAsync();

        if (!metaFileInfo.Exists)
        {
            return null;
        }

        var metadata = await ImageCacheMetadata.ReadAsync(blob.OpenRead());

        uri = new($"{fileId}{ToImageExtension(metadata)}");
        blob = blobFactory.GetBlob(uri);
        var fileInfo = await blob.AsFileInfoAsync();

        // Check to see if the file exists.
        return !fileInfo.Exists ? null : new BlobImageCacheResolver(fileInfo, metadata);
    }

    /// <inheritdoc/>
    public async Task SetAsync(string key, Stream stream, ImageCacheMetadata metadata)
    {

        var name = $"{_cacheOptions.Prefix}{key}";
        var imagefile = $"{name}{ToImageExtension(metadata)}";
        var metafile = $"{name}.meta";

        var blob = CreateBlob(imagefile);
        blob.Write(stream);

        blob = CreateBlob(metafile);
        await metadata.WriteAsync(blob.OpenWrite());
    }

    private FileBlob CreateBlob(string file)
    {
        var container = GetContainer();
        var path = Path.Combine(_cacheRootPath, container, file);

        Uri uri = new($"{Blob.BlobUriScheme}://{Blob.DefaultProvider}/{container}/{file}");
        FileBlob blob = new(uri, path);

        return blob;
    }

    private string GetContainer()
    {
        var url = _httpContextAccessor.HttpContext?.Request.Path.Value;
        var media = string.IsNullOrWhiteSpace(url) ? null : UrlResolver.Current.Route(new(url)) as MediaData;

        // We're working with a static file here
        var container = media?.BinaryDataContainer?.Segments[1] ?? $"_{_cacheOptions.Prefix}static";

        return container;
    }

    /// <summary>
    /// Gets the path to the image file based on the supplied root and metadata.
    /// </summary>
    /// <param name="metaData">The image metadata.</param>
    /// <returns>The <see cref="string"/>.</returns>
    private string ToImageExtension(in ImageCacheMetadata metaData)
        => $".{_formatUtilities.GetExtensionFromContentType(metaData.ContentType)}";
}