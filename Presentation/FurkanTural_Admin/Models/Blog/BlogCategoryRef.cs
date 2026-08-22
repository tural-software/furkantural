namespace FurkanTural_Admin.Models.Blog;

/// <summary>Bir bloğa atanmış kategorinin hafif gösterimi (satır JSON'u + chip için).</summary>
public sealed class BlogCategoryRef
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Color { get; set; }
}