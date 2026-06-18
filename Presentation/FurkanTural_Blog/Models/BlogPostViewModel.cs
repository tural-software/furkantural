using System.Globalization;

namespace FurkanTural_Blog.Models;

public class BlogPostViewModel
{
    private static readonly CultureInfo Tr = new("tr-TR");

    public int Id { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>Yayın tarihi, Türkçe uzun biçim (örn. "18 Haziran 2026"); tarih yoksa boş.</summary>
    public string PublishedDisplay =>
        CreatedAt == default ? string.Empty : CreatedAt.ToString("d MMMM yyyy", Tr);

    /// <summary>Makine-okur tarih (datetime attribute / JSON-LD için), ISO 8601; tarih yoksa boş.</summary>
    public string PublishedIso =>
        CreatedAt == default ? string.Empty : CreatedAt.ToString("yyyy-MM-dd");

    /// <summary>Yaklaşık okuma süresi (dakika), ~200 kelime/dk; en az 1.</summary>
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

    /// <summary>Listeleme için içerikten kısa bir özet üretir (düz metin, ~160 karakter).</summary>
    public string Excerpt
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Content))
                return string.Empty;

            var text = Content.Trim();
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
