namespace FurkanTural_Blog.Models;

/// <summary>
/// Url göreli yoldur ve tek başına kullanılamaz; tam adres API tabanıyla birleştirilerek
/// oluşturulur (bkz. <c>HomeController.BuildImageUrl</c>).
/// </summary>
public class BlogImageViewModel
{
    public string? Url { get; set; }
    public string? AltText { get; set; }
    public bool IsCover { get; set; }
    public int BlogId { get; set; }
}