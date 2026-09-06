namespace FurkanTural_API.Models.Category;

public class CreateCategoryRequest
{
    public string? Name { get; set; }
    public string? Color { get; set; }
}

public class UpdateCategoryRequest
{
    public int Id { get; set; }
    public string? Name { get; set; }

    /// <summary>Kalıcı adres parçası. Boş bırakılırsa mevcut adres korunur; dolu gönderilirse temizlenip tekilleştirilerek yazılır ve eski adres kırılır.</summary>
    public string? Slug { get; set; }
    public string? Color { get; set; }
}