using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
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
using SixLabors.ImageSharp.Web.Resolvers;

namespace Krlbr.Optimizely.ImageSharp.Web.Caching;

/// <summary>
/// Implements an Optimizely blob-based cache.
/// </summary>
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public class BlobImageCache : IImageCache
{
    /// <summary>
    /// The root path for the cache.
    /// </summary>
    private readonly string _cacheRootPath;

    /// <summary>
    /// The cache configuration options.
    /// </summary>
    private readonly BlobImageCacheOptions _cacheOptions;

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
    /// <param name="formatUtilities">Contains various format helper methods based on the current configuration.</param>
    /// <param name="httpContextAccessor">The HTTP context accessor.</param>
    public BlobImageCache(
        IOptions<BlobImageCacheOptions>? cacheOptions,
        IWebHostEnvironment environment,
        FormatUtilities formatUtilities,
        IHttpContextAccessor httpContextAccessor)
    {
        // Allow configuration of the cache without having to register everything.
        _cacheOptions = cacheOptions is not null ? cacheOptions.Value : new();

        _httpContextAccessor = httpContextAccessor;

        _cacheRootPath = Path.GetFullPath(Path.Combine(environment.ContentRootPath, _cacheOptions.CacheFolder));
        _formatUtilities = formatUtilities;
    }

    /// <inheritdoc/>
    public async Task<IImageCacheResolver?> GetAsync(string key)
    {
        var cancellationToken = _httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None;
        var fileName = $"{_cacheOptions.Prefix}{key}";
        var blob = CreateBlob($"{fileName}.meta");
        var exists = await blob.ExistsAsync(cancellationToken);
        if (!exists)
        {
            return null;
        }

        await using var readStream = await blob.OpenReadAsync(cancellationToken);
        var metadata = await ImageCacheMetadata.ReadAsync(readStream);

        blob = CreateBlob($"{fileName}{ToImageExtension(metadata)}");
        exists = await blob.ExistsAsync(cancellationToken);
        if (!exists)
        {
            return null;
        }

        var fileInfo = await blob.AsFileInfoAsync(default, false, cancellationToken);
        return new BlobImageCacheResolver(fileInfo, metadata);
    }

    /// <inheritdoc/>
    public async Task SetAsync(string key, Stream stream, ImageCacheMetadata metadata)
    {
        var cancellationToken = _httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None;
        var name = $"{_cacheOptions.Prefix}{key}";
        var imageFile = $"{name}{ToImageExtension(metadata)}";
        var metafile = $"{name}.meta";

        var blob = CreateBlob(imageFile);
        await blob.WriteAsync(stream, cancellationToken);

        blob = CreateBlob(metafile);
        await using var writeStream = await blob.OpenWriteAsync(cancellationToken);
        await metadata.WriteAsync(writeStream);
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
        var urlResolver = _httpContextAccessor.HttpContext?.RequestServices.GetInstance<IUrlResolver>();
        var media = string.IsNullOrWhiteSpace(url) ? null : urlResolver.Route(new(url)) as MediaData;

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