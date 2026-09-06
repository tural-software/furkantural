namespace FurkanTural_Admin.Models.Blog;

public sealed class BlogFormDto
{
    public string? Title { get; set; }
    /// <summary>Kalıcı adres parçası. Boş bırakılırsa mevcut adres korunur; dolu gönderilirse eski adres kırılır ve bu bilinçli bir karardır.</summary>
    public string? Slug { get; set; }
    public string? Content { get; set; }
    public List<int>? CategoryIds { get; set; }
}