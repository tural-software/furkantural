namespace FurkanTural_API.Models.ContactTemplate;

public class CreateContactTemplateRequest
{
    public string? Name { get; set; }
    public string? TemplateType { get; set; }
    public string? FileName { get; set; }
    public string? HtmlContent { get; set; }
}

public class UpdateContactTemplateRequest
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? TemplateType { get; set; }
    public string? FileName { get; set; }
    public string? HtmlContent { get; set; }
}
