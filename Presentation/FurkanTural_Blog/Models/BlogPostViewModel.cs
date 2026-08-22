using System.Globalization;
using FurkanTural_Blog.Helpers;
using Microsoft.AspNetCore.Html;

namespace FurkanTural_Blog.Models;

public class BlogPostViewModel
{
    private static readonly CultureInfo Tr = new("tr-TR");

    public int Id { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Denetleyici tarafından doldurulur; API'den gelen görselin göreli yolu burada tam adrese
    /// çevrilmiş hâlde durur.
    /// </summary>
    public string? CoverImageUrl { get; set; }

    public string? CoverAltText { get; set; }

    public List<CategoryViewModel> Categories { get; set; } = [];

    /// <summary>
    /// Okunabilir tarih Türkçe kültürle biçimlenir; sunucunun kültür ayarından bağımsız olsun diye
    /// kültür koda sabitlenmiştir.
    /// </summary>
    public string PublishedDisplay =>
        CreatedAt == default ? string.Empty : CreatedAt.ToString("d MMMM yyyy", Tr);

    /// <summary>
    /// Makine tarafında okunan biçim; sayfadaki tarih etiketleri ve yapılandırılmış veri bunu
    /// kullanır, ekranda görünen metni değil.
    /// </summary>
    public string PublishedIso =>
        CreatedAt == default ? string.Empty : CreatedAt.ToString("yyyy-MM-dd");

    /// <summary>
    /// Hiç düzenlenmemiş yazıda yayın tarihine düşer, boş kalmaz: arama motorları bu alanın
    /// yokluğunu değil değerini bekler.
    /// </summary>
    public string ModifiedIso
    {
        get
        {
            var d = UpdatedAt ?? CreatedAt;
            return d == default ? string.Empty : d.ToString("yyyy-MM-dd");
        }
    }

    public IHtmlContent ContentHtml => MarkdownRenderer.ToHtml(Content);

    /// <summary>
    /// Dakikada iki yüz kelime varsayımıyla hesaplanır ve hiçbir zaman sıfır dönmez; çok kısa bir
    /// yazı da "1 dakika" gösterir.
    /// </summary>
    public int ReadingMinutes
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Content))
                return 1;
            var words = Content.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
            return Math.Max(1, (int)Math.Ceiling(words / 200.0));
        }
    }

    /// <summary>
    /// Markdown işaretleri atılıp düz metne indirgenir, sonra yüz altmış karakterde kesilir. Kesme
    /// son boşluğa çekilir ki özet kelimenin ortasında bitmesin.
    /// </summary>
    public string Excerpt
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Content))
                return string.Empty;

            var text = MarkdownRenderer.ToPlainText(Content);
            if (text.Length == 0)
                return string.Empty;
            const int max = 160;
            if (text.Length <= max)
                return text;

            var cut = text[..max];
            var lastSpace = cut.LastIndexOf(' ');
            if (lastSpace > 0)
                cut = cut[..lastSpace];
            return cut + "…";
        }
    }
}