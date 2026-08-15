namespace FurkanTural_Application.DTOs.ContactTemplate;

public class UpdateContactTemplateDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? TemplateType { get; set; }
    public string? FileName { get; set; }
    public string? HtmlContent { get; set; }
    public int? UpdatedBy { get; set; }
}