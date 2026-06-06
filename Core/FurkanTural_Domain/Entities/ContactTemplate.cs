using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Domain.Entities;

public class ContactTemplate : BaseEntity
{
    public string? Name { get; set; }
    public string? TemplateType { get; set; }
    public string? FileName { get; set; }
    public string? HtmlContent { get; set; }
}
