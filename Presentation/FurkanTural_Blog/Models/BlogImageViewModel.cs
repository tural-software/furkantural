namespace FurkanTural_Blog.Models;

// API'den gelen blog görseli (BlogImage). Url göreli yol (örn. "blog/images/x.jpg");
// tam adres = ApiBaseUrl + "/" + Url.
public class BlogImageViewModel
{
    public string? Url { get; set; }
    public string? AltText { get; set; }
    public bool IsCover { get; set; }
    public int BlogId { get; set; }
}
