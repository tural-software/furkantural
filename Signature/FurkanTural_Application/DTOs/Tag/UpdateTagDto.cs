namespace FurkanTural_Application.DTOs.Tag;

/// <summary>Etiket güncelleme. Slug boş bırakılırsa mevcut adres korunur; dolu gönderilirse temizlenip tekilleştirilerek yazılır ve eski adres kırılır.</summary>
public class UpdateTagDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Slug { get; set; }
}
