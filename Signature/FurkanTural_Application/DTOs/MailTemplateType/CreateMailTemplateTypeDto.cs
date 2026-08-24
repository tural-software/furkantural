namespace FurkanTural_Application.DTOs.MailTemplateType;

public class CreateMailTemplateTypeDto
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public int? CreatedBy { get; set; }
}
