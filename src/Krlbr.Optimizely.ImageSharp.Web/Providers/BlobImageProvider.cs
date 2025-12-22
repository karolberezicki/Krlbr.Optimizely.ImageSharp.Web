using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using EPiServer.Core;
using EPiServer.Web;
using EPiServer.Web.Routing;
using Krlbr.Optimizely.ImageSharp.Web.Resolvers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using SixLabors.ImageSharp.Web;
using SixLabors.ImageSharp.Web.Providers;
using SixLabors.ImageSharp.Web.Resolvers;

namespace Krlbr.Optimizely.ImageSharp.Web.Providers;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
[SuppressMessage("ReSharper", "ReplaceAutoPropertyWithComputedProperty")]
public class BlobImageProvider : IImageProvider
{

    /// <summary>
    /// A match function used by the resolver to identify itself as the correct resolver to use.
    /// </summary>
    private Func<HttpContext, bool>? _match;

    /// <summary>
    /// Contains various format helper methods based on the current configuration.
    /// </summary>
    private readonly FormatUtilities _formatUtilities;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlobImageProvider"/> class.
    /// </summary>
    /// <param name="formatUtilities">Contains various format helper methods based on the current configuration.</param>
    public BlobImageProvider(FormatUtilities formatUtilities)
    {
        _formatUtilities = formatUtilities;
    }

    /// <inheritdoc/>
    public ProcessingBehavior ProcessingBehavior { get; } = ProcessingBehavior.CommandOnly;

    /// <inheritdoc/>
    public Func<HttpContext, bool> Match
    {
        get => _match ?? IsMatch;
        set => _match = value;
    }

    /// <inheritdoc/>
    public bool IsValidRequest(HttpContext context) => _formatUtilities.TryGetExtensionFromUri(context.Request.GetDisplayUrl(), out _);

    /// <inheritdoc/>
    public Task<IImageResolver?> GetAsync(HttpContext context)
    {
        var url = context.Request.Path.Value;

        if (UrlResolver.Current.Route(new(url)) is MediaData { BinaryData: not null } media)
        {
            return Task.FromResult<IImageResolver?>(new BlobImageResolver(media));
        }
        return Task.FromResult<IImageResolver?>(null);
    }

    private bool IsMatch(HttpContext context)
    {
        var matchMediaUrlSegments = MediaUrlSegments
            .Any(p => context.Request.Path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase));
        return matchMediaUrlSegments;
    }

    private static readonly HashSet<string> MediaUrlSegments = new(StringComparer.OrdinalIgnoreCase)
    {
        $"/{RoutingConstants.ContentAssetSegment}",
        $"/{RoutingConstants.SiteAssetSegment}",
        $"/{RoutingConstants.GlobalAssetSegment}",
        $"/{SiteDefinition.SiteAssetsName}",
    };
}