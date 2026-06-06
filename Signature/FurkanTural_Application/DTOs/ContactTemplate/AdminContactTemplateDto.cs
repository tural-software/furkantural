namespace FurkanTural_Application.DTOs.ContactTemplate;

public class AdminContactTemplateDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? TemplateType { get; set; }
    public string? FileName { get; set; }
    public string? HtmlContent { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
}
