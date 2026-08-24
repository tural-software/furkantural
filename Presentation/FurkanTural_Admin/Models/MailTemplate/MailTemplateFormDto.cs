namespace FurkanTural_Admin.Models.MailTemplate;

public sealed class MailTemplateFormDto
{
    public int MailTemplateTypeId { get; set; }
    public string? Name { get; set; }
    public string? Subject { get; set; }
    public string? HtmlContent { get; set; }
    public string? FileName { get; set; }
}
