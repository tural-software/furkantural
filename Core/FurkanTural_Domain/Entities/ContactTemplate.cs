using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Domain.Entities;

/// <summary>
/// <see cref="Contact"/> akışında kullanılan e-posta şablonu. TemplateType "Owner" (site sahibine
/// düşen bildirim) veya "User" (gönderene giden yanıt) değerini alır; ContactService şablonu bu
/// değerle seçer.
/// </summary>
public class ContactTemplate : BaseEntity
{
    public string? Name { get; set; }
    public string? TemplateType { get; set; }
    public string? FileName { get; set; }
    public string? HtmlContent { get; set; }
}
