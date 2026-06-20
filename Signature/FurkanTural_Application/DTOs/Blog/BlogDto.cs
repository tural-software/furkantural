using FurkanTural_Application.DTOs.Category;

namespace FurkanTural_Application.DTOs.Blog;

public class BlogDto
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>Bloğa atanmış kategoriler (kart chip'leri için).</summary>
    public List<CategoryDto> Categories { get; set; } = [];
}
