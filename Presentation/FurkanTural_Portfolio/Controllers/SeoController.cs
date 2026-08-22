using System.Text;
using System.Xml;
using Microsoft.AspNetCore.Mvc;
using FurkanTural_Portfolio.Services;

namespace FurkanTural_Portfolio.Controllers;

/// <summary>
/// Arama motorlarının beklediği robots.txt ve sitemap.xml dosyalarını üretir. Adresler
/// yapılandırmadan değil isteğin kendi host'undan kurulur, yani site hangi alan adından sunuluyorsa
/// çıktı da onu gösterir.
///
/// Sitemap proje ve müzik detay sayfalarını da listeler; API'ye ulaşılamazsa hata dönmez, dosya
/// yalnızca statik sayfalarla üretilir. Arama motoruna hata vermek, eksik ama geçerli bir dosya
/// vermekten kötüdür.
///
/// XML çıktısı metin olarak değil bayt olarak döndürülür. Bir metin oluşturucuya yazıldığında
/// bildirim her zaman utf-16 çıkar ve dosyanın kendi başlığı gerçek kodlamasıyla çelişirdi.
/// </summary>
public class SeoController(IPortfolioApiService apiService) : Controller
{
    private readonly IPortfolioApiService _apiService = apiService;

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
            ($"{baseUrl}/", "1.0", "monthly"),
            ($"{baseUrl}/Home/Privacy", "0.3", "yearly"),
        };

        var projectsTask = _apiService.GetProjectsAsync(cancellationToken);
        var songsTask = _apiService.GetSongsAsync(cancellationToken);
        await Task.WhenAll(projectsTask, songsTask);

        foreach (var project in projectsTask.Result)
        {
            urls.Add(($"{baseUrl}/Projects/Detail/{project.Id}", "0.7", "monthly"));
        }
        foreach (var song in songsTask.Result)
        {
            urls.Add(($"{baseUrl}/Music/Detail/{song.Id}", "0.7", "monthly"));
        }

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