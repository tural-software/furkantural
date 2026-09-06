namespace FurkanTural_Admin.Models.Category;

public sealed class CategoryFormDto
{
    public string? Name { get; set; }
    /// <summary>Kalıcı adres parçası. Boş bırakılırsa mevcut adres korunur; dolu gönderilirse eski adres kırılır ve bu bilinçli bir karardır.</summary>
    public string? Slug { get; set; }
    public string? Color { get; set; }
}