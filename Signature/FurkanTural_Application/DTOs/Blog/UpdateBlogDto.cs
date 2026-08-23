namespace FurkanTural_Application.DTOs.Blog;

/// <summary>Blog güncelleme. CategoryIds üç durumludur ve null ile boş liste aynı şey değildir: null gönderilirse kategorilere hiç dokunulmaz, boş liste gönderilirse hepsi kaldırılır, dolu liste gönderilirse bağlar tam olarak o listeye eşitlenir. Title ve Content boş geçilemez.</summary>
public class UpdateBlogDto
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public int? UpdatedBy { get; set; }
    public List<int>? CategoryIds { get; set; }
}
