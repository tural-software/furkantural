namespace FurkanTural_Admin.Models.ContactTemplate;

public sealed class ContactTemplateAdminDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? TemplateType { get; set; }
    public string? FileName { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
}