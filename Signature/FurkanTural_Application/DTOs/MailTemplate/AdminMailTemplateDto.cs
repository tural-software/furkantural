namespace FurkanTural_Application.DTOs.MailTemplate;

/// <summary>Yönetim görünümü; gövdeyi ve denetim alanlarını da taşır. Placeholders türün gövde DTO'sundan çalışma anında üretilir, elle tutulan bir liste değildir — şablonu düzenleyen kişi böylece gerçekten değiştirilecek alanları görür.</summary>
public class AdminMailTemplateDto
{
    public int Id { get; set; }
    public int MailTemplateTypeId { get; set; }
    public string? TypeCode { get; set; }
    public string? TypeName { get; set; }
    public string? Name { get; set; }
    public string? Subject { get; set; }
    public string? HtmlContent { get; set; }
    public string? FileName { get; set; }
    public IReadOnlyList<string> Placeholders { get; set; } = [];
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
}
