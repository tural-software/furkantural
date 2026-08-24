namespace FurkanTural_API.Models.MailTemplateType;

public class CreateMailTemplateTypeRequest
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
}

public class UpdateMailTemplateTypeRequest
{
    public int Id { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
}
