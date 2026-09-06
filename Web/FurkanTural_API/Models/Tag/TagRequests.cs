namespace FurkanTural_API.Models.Tag;

public class CreateTagRequest
{
    public string? Name { get; set; }
}

public class UpdateTagRequest
{
    public int Id { get; set; }
    public string? Name { get; set; }

    /// <summary>Kalıcı adres parçası. Boş bırakılırsa mevcut adres korunur; dolu gönderilirse temizlenip tekilleştirilerek yazılır ve eski adres kırılır.</summary>
    public string? Slug { get; set; }
}
