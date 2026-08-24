namespace FurkanTural_Admin.Models.MailTemplate;

/// <summary>Şablon formundaki proje listesi. Liste salt okunurdur; yeni proje panelden değil, çözüme yeni bir sunum projesi girdiğinde tohumla eklenir.</summary>
public sealed class AppSourceOptionDto
{
    public int Id { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
}
