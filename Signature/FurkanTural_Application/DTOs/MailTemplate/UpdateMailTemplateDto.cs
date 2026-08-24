namespace FurkanTural_Application.DTOs.MailTemplate;

public class UpdateMailTemplateDto
{
    public int Id { get; set; }
    public int MailTemplateTypeId { get; set; }
    public int? AppSourceId { get; set; }
    public string? Name { get; set; }
    public string? Subject { get; set; }
    public string? HtmlContent { get; set; }
    public string? FileName { get; set; }
    public int? UpdatedBy { get; set; }
}
