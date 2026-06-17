using System.Text;
using System.Xml;
using FurkanTural_Blog.Services;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_Blog.Controllers;

/// <summary>
/// Arama motorları için robots.txt ve sitemap.xml üretir.
/// URL'ler isteğin host'una göre mutlak olarak oluşturulur; sitemap statik
/// sayfaların yanında yayınlı blog yazılarını da içerir (API erişilemezse
/// yalnız statik sayfalar listelenir).
/// </summary>
public class SeoController(IBlogApiService blogApi) : Controller
{
    private readonly IBlogApiService _blogApi = blogApi;

    [HttpGet("robots.txt")]
    [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Any)]
    public IActionResult Robots()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var sb = new StringBuilder();
        sb.AppendLine("User-agent: *");
        sb.AppendLine("Allow: /");
        sb.AppendLine("Disallow: /Home/Error");
        sb.AppendLine();
        sb.AppendLine($"Sitemap: {baseUrl}/sitemap.xml");
        return Content(sb.ToString(), "text/plain", Encoding.UTF8);
    }

    [HttpGet("sitemap.xml")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Sitemap(CancellationToken cancellationToken)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");

        var urls = new List<(string Loc, string Priority, string ChangeFreq)>
        {
            ($"{baseUrl}/", "1.0", "weekly"),
            ($"{baseUrl}/Home/Privacy", "0.3", "yearly"),
        };

        // Yayınlı blog yazıları (API erişilemezse boş liste → yalnız statik sayfalar).
        var posts = await _blogApi.GetPostsAsync(cancellationToken);
        foreach (var post in posts)
            urls.Add(($"{baseUrl}/Home/Post/{post.Id}", "0.7", "monthly"));

        // XmlWriter bir StringBuilder'a yazınca bildirimi daima utf-16 olur;
        // doğru "encoding=utf-8" bildirimi için UTF-8 stream'e yazıp byte döndürüyoruz.
        using var ms = new MemoryStream();
        var settings = new XmlWriterSettings { Indent = true, Encoding = new UTF8Encoding(false) };
        using (var writer = XmlWriter.Create(ms, settings))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");
            foreach (var (loc, priority, changeFreq) in urls)
            {
                writer.WriteStartElement("url");
                writer.WriteElementString("loc", loc);
                writer.WriteElementString("lastmod", today);
                writer.WriteElementString("changefreq", changeFreq);
                writer.WriteElementString("priority", priority);
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return File(ms.ToArray(), "application/xml; charset=utf-8");
    }
}
