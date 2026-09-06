namespace FurkanTural_Application.DTOs.Category;

public class CategoryDto
{
    public int Id { get; set; }
    public string? Name { get; set; }

    /// <summary>Kalıcı adres parçası. Kategori adı değişse de adres durur.</summary>
    public string? Slug { get; set; }
    public string? Color { get; set; }
}