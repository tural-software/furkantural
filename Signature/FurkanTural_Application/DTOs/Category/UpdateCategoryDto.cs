namespace FurkanTural_Application.DTOs.Category;

public class UpdateCategoryDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Color { get; set; }

    /// <summary>Adresi elle düzeltmek için. Boş bırakılırsa mevcut adres korunur; dolu gönderilirse temizlenip tekilleştirilerek yazılır — bilinçli bir karardır ve eski adresi kırar.</summary>
    public string? Slug { get; set; }
    public int? UpdatedBy { get; set; }
}