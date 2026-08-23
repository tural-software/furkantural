using System.Text;
using System.Xml;
using FurkanTural_Blog.Services;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_Blog.Controllers;

/// <summary>Arama motorlarının ve besleme okuyucularının beklediği üç dosyayı üretir: robots.txt, sitemap.xml ve feed.xml. Adresler yapılandırmadan değil isteğin kendi host'undan kurulur, yani site hangi alan adından sunuluyorsa çıktı da onu gösterir.<para>Üçü de API'ye bağımlıdır ama hiçbiri API'ye bağlı kalmaz: yazılar alınamazsa sitemap yalnızca statik sayfaları, besleme ise boş bir kanal döner. Arama motoruna hata vermek, eksik ama geçerli bir dosya vermekten kötüdür.</para><para>XML çıktıları metin olarak değil bayt olarak döndürülür. Bir metin oluşturucuya yazıldığında bildirim her zaman utf-16 çıkar ve dosyanın kendi başlığı gerçek kodlamasıyla çelişirdi.</para></summary>
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

    /// <summary>Anasayfanın son değişiklik tarihi en güncel yazınınkinden alınır, çünkü sayfa yazıları listeler ve kendi başına bir değişiklik tarihi yoktur. Gizlilik sayfası sabit bir tarih taşır; içeriği değişirse buradaki değer de elle güncellenmelidir.</summary>
    [HttpGet("sitemap.xml")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Sitemap(CancellationToken cancellationToken)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        static string Iso(DateTime d) => DateTime.SpecifyKind(d, DateTimeKind.Utc).ToString("yyyy-MM-dd");

        var posts = await _blogApi.GetSitemapItemsAsync(cancellationToken);

        var homeLastMod = posts.Count > 0 ? posts.Max(p => p.LastModified) : DateTime.UtcNow;

        var urls = new List<(string Loc, string LastMod, string Priority, string ChangeFreq)>
        {
            ($"{baseUrl}/", Iso(homeLastMod), "1.0", "weekly"),
            ($"{baseUrl}/Home/Privacy", "2026-01-01", "0.3", "yearly"),
        };

        foreach (var post in posts)
            urls.Add(($"{baseUrl}/Home/Post/{post.Id}", Iso(post.LastModified), "0.7", "monthly"));

        using var ms = new MemoryStream();
        var settings = new XmlWriterSettings { Indent = true, Encoding = new UTF8Encoding(false) };
        using (var writer = XmlWriter.Create(ms, settings))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");
            foreach (var (loc, lastMod, priority, changeFreq) in urls)
            {
                writer.WriteStartElement("url");
                writer.WriteElementString("loc", loc);
                writer.WriteElementString("lastmod", lastMod);
                writer.WriteElementString("changefreq", changeFreq);
                writer.WriteElementString("priority", priority);
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return File(ms.ToArray(), "application/xml; charset=utf-8");
    }

    /// <summary>Beslemede en yeni yirmi yazı yer alır. Sınır okuyucu tarafında bir gereklilik değil, dosya boyutunu sabit tutmak içindir; arşivin tamamı sitemap üzerinden zaten erişilebilir.</summary>
    [HttpGet("feed.xml")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Feed(CancellationToken cancellationToken)
    {
        const string atomNs = "http://www.w3.org/2005/Atom";
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var nowR = DateTime.UtcNow.ToString("r");

        var posts = (await _blogApi.GetPostsAsync(cancellationToken))
            .OrderByDescending(p => p.CreatedAt)
            .Take(20)
            .ToList();

        using var ms = new MemoryStream();
        var settings = new XmlWriterSettings { Indent = true, Encoding = new UTF8Encoding(false) };
        using (var writer = XmlWriter.Create(ms, settings))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("rss");
            writer.WriteAttributeString("version", "2.0");
            writer.WriteAttributeString("xmlns", "atom", null, atomNs);

            writer.WriteStartElement("channel");
            writer.WriteElementString("title", "Furkan Tural Blog");
            writer.WriteElementString("link", $"{baseUrl}/");
            writer.WriteElementString("description", "Furkan Tural'ın yazılım, teknoloji ve geliştirme üzerine notları.");
            writer.WriteElementString("language", "tr-TR");
            writer.WriteElementString("lastBuildDate", nowR);

            writer.WriteStartElement("atom", "link", atomNs);
            writer.WriteAttributeString("href", $"{baseUrl}/feed.xml");
            writer.WriteAttributeString("rel", "self");
            writer.WriteAttributeString("type", "application/rss+xml");
            writer.WriteEndElement();

            foreach (var post in posts)
            {
                var link = $"{baseUrl}/Home/Post/{post.Id}";
                writer.WriteStartElement("item");
                writer.WriteElementString("title", post.Title ?? string.Empty);
                writer.WriteElementString("link", link);
                if (!string.IsNullOrWhiteSpace(post.Excerpt))
                    writer.WriteElementString("description", post.Excerpt);
                writer.WriteElementString("pubDate", DateTime.SpecifyKind(post.CreatedAt, DateTimeKind.Utc).ToString("r"));
                writer.WriteStartElement("guid");
                writer.WriteAttributeString("isPermaLink", "true");
                writer.WriteString(link);
                writer.WriteEndElement();
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return File(ms.ToArray(), "application/rss+xml; charset=utf-8");
    }
}
