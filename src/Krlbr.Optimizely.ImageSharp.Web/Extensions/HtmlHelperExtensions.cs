using System;
using System.Diagnostics.CodeAnalysis;
using EPiServer;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using EPiServer.Web.Routing;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Krlbr.Optimizely.ImageSharp.Web.Extensions;

[SuppressMessage("ReSharper", "UnusedMember.Global")]
public static class HtmlHelperExtensions
{
    extension(IHtmlHelper htmlHelper)
    {
        public UrlBuilder ProcessImage(ContentReference image)
        {
            if (image == null || image == ContentReference.EmptyReference)
            {
                throw new ArgumentNullException(nameof(image), "You might want to use `ProcessImageWithFallback()` instead");
            }

            var urlResolver = htmlHelper.ViewContext.HttpContext.RequestServices.GetInstance<IUrlResolver>();
            var url = urlResolver?.GetUrl(image);

            return ConstructUrl(url);
        }

        public UrlBuilder ProcessImageWithFallback(ContentReference? image, string imageFallback)
        {
            var urlResolver = htmlHelper.ViewContext.HttpContext.RequestServices.GetInstance<IUrlResolver>();
            return ConstructUrl(image is null || image == ContentReference.EmptyReference ? imageFallback : urlResolver?.GetUrl(image));
        }

        public static UrlBuilder ProcessImage(string? imageUrl)
        {
            return string.IsNullOrEmpty(imageUrl)
                ? throw new ArgumentNullException(nameof(imageUrl), "You might want to use `ProcessImageWithFallback()` instead")
                : ConstructUrl(imageUrl);
        }

        public static UrlBuilder ProcessImageWithFallback(string? imageUrl, string? imageFallback)
        {
            return ConstructUrl(string.IsNullOrEmpty(imageUrl) ? imageFallback : imageUrl);
        }
    }

    private static UrlBuilder ConstructUrl(string? url)
    {
        var builder = new UrlBuilder(url);
        return builder;
    }
}