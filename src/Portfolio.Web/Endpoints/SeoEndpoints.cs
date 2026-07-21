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
            var posts = await blog.GetPublishedAsync();

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

        app.MapGet("/sitemap.xml", async (HttpContext ctx, BlogService blog, IConfiguration config) =>
        {
            var baseUrl = BaseUrl(ctx, config);
            XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

            var urls = new List<XElement>();
            foreach (var path in new[] { "/", "/projects", "/blog", "/contact", "/terms", "/privacy" })
            {
                urls.Add(new XElement(ns + "url", new XElement(ns + "loc", $"{baseUrl}{path.TrimEnd('/')}")));
            }

            foreach (var post in await blog.GetPublishedAsync())
            {
                urls.Add(new XElement(ns + "url",
                    new XElement(ns + "loc", $"{baseUrl}/blog/{post.Slug}"),
                    new XElement(ns + "lastmod", post.UpdatedAt.ToString("yyyy-MM-dd"))));
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
    }

    /// <summary>PUBLIC_BASE_URL when configured (canonical), otherwise the request origin.</summary>
    private static string BaseUrl(HttpContext ctx, IConfiguration config)
    {
        var configured = config["PUBLIC_BASE_URL"];
        return string.IsNullOrWhiteSpace(configured)
            ? $"{ctx.Request.Scheme}://{ctx.Request.Host}"
            : configured.TrimEnd('/');
    }

    private static string Declaration(XDocument doc)
        => $"<?xml version=\"1.0\" encoding=\"utf-8\"?>{Environment.NewLine}{doc}";
}
