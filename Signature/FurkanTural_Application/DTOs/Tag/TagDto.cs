namespace FurkanTural_Application.DTOs.Tag;

public class TagDto
{
    public int Id { get; set; }
    public string? Name { get; set; }

    /// <summary>Kalıcı adres parçası. Etiket adı değişse de adres durur.</summary>
    public string? Slug { get; set; }
}
