namespace FurkanTural_Application.DTOs.BlogImage;

/// <summary>
/// Blog yazısının görseli. IsCover tekil değildir: yeni bir görseli kapak işaretlemek eskisinin
/// işaretini kaldırmaz, hiçbir yerde teklik denetimi yapılmaz. Aynı yazıda birden çok kapak
/// oluşmaması çağıranın sorumluluğundadır.
/// </summary>
public class BlogImageDto
{
    public int Id { get; set; }
    public string? Url { get; set; }
    public string? AltText { get; set; }
    public bool IsCover { get; set; }
    public int BlogId { get; set; }
}