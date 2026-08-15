using System.Text;
using System.Xml.Linq;
using Portfolio.Web.Services;

namespace Portfolio.Web.Endpoints;

public static class SeoEndpoints
{
    public static void MapSeoEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/feed.xml", async (HttpContext ctx, BlogService blog, SiteConfig site, IConfiguration config) =>
        {
            var baseUrl = BaseUrl(ctx, config);
            var posts = await blog.GetLatestPublishedAsync(20);

            XNamespace atom = "http://www.w3.org/2005/Atom";
            var channel = new XElement("channel",
                new XElement("title", site.SiteTitle),
                new XElement("link", baseUrl),
                new XElement("description", site.Tagline),
                new XElement(atom + "link",
                    new XAttribute("href", $"{baseUrl}/feed.xml"),
                    new XAttribute("rel", "self"),
                    new XAttribute("type", "application/rss+xml")));

            foreach (var post in posts)
            {
                var url = $"{baseUrl}/blog/{post.Slug}";
                channel.Add(new XElement("item",
                    new XElement("title", post.Title),
                    new XElement("link", url),
                    new XElement("guid", new XAttribute("isPermaLink", "true"), url),
                    new XElement("pubDate", (post.PublishedAt ?? post.CreatedAt).ToString("R")),
                    new XElement("description", post.Summary)));
            }

            var rss = new XDocument(
                new XElement("rss",
                    new XAttribute("version", "2.0"),
                    new XAttribute(XNamespace.Xmlns + "atom", atom),
                    channel));

            return Results.Content(Declaration(rss), "application/rss+xml", Encoding.UTF8);
        });

        // Config-gated like /resume: no OWNER_PHOTO_FILE, no photo anywhere.
        // Immutable caching is safe because renders always link it as
        // /owner-photo?v={write-time ticks}.
        app.MapGet("/owner-photo", async (HttpContext ctx, SiteConfig site) =>
        {
            var path = site.OwnerPhotoFile;
            if (path is null || !File.Exists(path))
            {
                return Results.NotFound();
            }

            var header = new byte[12];
            int read;
            await using (var probe = File.OpenRead(path))
            {
                read = await probe.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false);
            }

            // Sniffed, not trusted from the extension — the owner can copy any
            // file over the mount; refuse to serve bytes we can't identify.
            var contentType = OwnerPhotoService.SniffContentType(header.AsSpan(0, read));
            if (contentType is null)
            {
                return Results.NotFound();
            }

            ctx.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
            return Results.File(path, contentType);
        });

        app.MapGet("/sitemap.xml", async (HttpContext ctx, BlogService blog, IConfiguration config) =>
        {
            var baseUrl = BaseUrl(ctx, config);
            XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

            var urls = new List<XElement>();
            foreach (var path in new[] { "/", "/projects", "/blog", "/contact", "/terms", "/privacy" })
            {
                urls.Add(new XElement(ns + "url", new XElement(ns + "loc", $"{baseUrl}{path.TrimEnd('/')}")));
            }

            foreach (var (slug, updatedAt) in await blog.GetPublishedSlugsAsync())
            {
                urls.Add(new XElement(ns + "url",
                    new XElement(ns + "loc", $"{baseUrl}/blog/{slug}"),
                    new XElement(ns + "lastmod", updatedAt.ToString("yyyy-MM-dd"))));
            }

            var sitemap = new XDocument(new XElement(ns + "urlset", urls));
            return Results.Content(Declaration(sitemap), "application/xml", Encoding.UTF8);
        });

        app.MapGet("/robots.txt", (HttpContext ctx, IConfiguration config) =>
        {
            var baseUrl = BaseUrl(ctx, config);
            return Results.Text($"""
                User-agent: *
                Disallow: /admin
                Disallow: /auth
                Disallow: /signin
                Allow: /

                Sitemap: {baseUrl}/sitemap.xml
                """);
        });

        app.MapGet("/site.webmanifest", async (SiteConfig site, ThemeService theme) =>
        {
            var snapshot = await theme.GetSnapshotAsync();
            return Results.Content(
                SeoRules.WebManifest(site.SiteTitle, site.OwnerName, snapshot.MetaThemeColor),
                "application/manifest+json",
                Encoding.UTF8);
        });
    }

    /// <summary>PUBLIC_BASE_URL when configured (canonical), otherwise the request origin.</summary>
    private static string BaseUrl(HttpContext ctx, IConfiguration config)
        => SeoRules.CanonicalOrigin(config["PUBLIC_BASE_URL"], $"{ctx.Request.Scheme}://{ctx.Request.Host}");

    private static string Declaration(XDocument doc)
        => $"<?xml version=\"1.0\" encoding=\"utf-8\"?>{Environment.NewLine}{doc}";
}
