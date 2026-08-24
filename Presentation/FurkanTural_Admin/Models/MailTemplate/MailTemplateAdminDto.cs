namespace FurkanTural_Admin.Models.MailTemplate;

/// <summary>API'deki AdminMailTemplateDto'nun yerel kopyası. Placeholders sunucuda türün gövde DTO'sundan üretilir; panel bu listeyi olduğu gibi gösterir ve kendi yanında ikinci bir liste tutmaz, aksi hâlde ikisi zamanla ayrışırdı.</summary>
public sealed class MailTemplateAdminDto
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
