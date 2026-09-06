namespace FurkanTural_Application.DTOs.Blog;

/// <summary>Blog güncelleme. CategoryIds ile TagIds üç durumludur ve null ile boş liste aynı şey değildir: null gönderilirse o bağa hiç dokunulmaz, boş liste gönderilirse hepsi kaldırılır, dolu liste gönderilirse bağlar tam olarak o listeye eşitlenir. Title ve Content boş geçilemez.</summary>
public class UpdateBlogDto
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }

    /// <summary>Adresi elle düzeltmek için. Boş bırakılırsa mevcut adres korunur; dolu gönderilirse temizlenip tekilleştirilerek yazılır — bilinçli bir karardır ve eski adresi kırar.</summary>
    public string? Slug { get; set; }
    public int? UpdatedBy { get; set; }
    public List<int>? CategoryIds { get; set; }
    public List<int>? TagIds { get; set; }
}
