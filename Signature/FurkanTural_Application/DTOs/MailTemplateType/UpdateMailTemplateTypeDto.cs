namespace FurkanTural_Application.DTOs.MailTemplateType;

public class UpdateMailTemplateTypeDto
{
    public int Id { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public int? UpdatedBy { get; set; }
}
