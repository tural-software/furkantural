namespace FurkanTural_Application.DTOs.Tag;

/// <summary>Yönetim görünümü. PostCount o etikete bağlı yayındaki yazı sayısıdır ve tek okumada toplanır; etiket başına ayrı sorgu, listenin kendisi kadar sorgu açardı.</summary>
public class AdminTagDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Slug { get; set; }
    public int PostCount { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
    public int? DeletedBy { get; set; }
}
