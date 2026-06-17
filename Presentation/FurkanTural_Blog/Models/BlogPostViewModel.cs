namespace FurkanTural_Blog.Models;

public class BlogPostViewModel
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }

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
