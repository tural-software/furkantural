namespace FurkanTural_Application.DTOs.MailTemplate;

/// <summary>Şablonun liste görünümü; gövde taşımaz. TypeCode, şablonun hangi gönderim yoluna bağlı olduğunu gösterir ve kod tarafının şablonu bulmak için kullandığı anahtarın aynısıdır.</summary>
public class MailTemplateDto
{
    public int Id { get; set; }
    public int MailTemplateTypeId { get; set; }
    public string? TypeCode { get; set; }
    public string? TypeName { get; set; }
    public int? AppSourceId { get; set; }
    public string? AppSourceName { get; set; }
    public string? Name { get; set; }
    public string? Subject { get; set; }
    public string? FileName { get; set; }
}
