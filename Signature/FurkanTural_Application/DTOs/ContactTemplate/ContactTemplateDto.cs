namespace FurkanTural_Application.DTOs.ContactTemplate;

/// <summary>
/// İletişim akışındaki e-posta şablonunun liste görünümü; gövde taşımaz, yalnızca yönetim DTO'larında
/// bulunur. Şablon adla değil TemplateType ile seçilir: "Owner" site sahibine düşen bildirimi, "User"
/// gönderene giden yanıtı temsil eder.
/// </summary>
public class ContactTemplateDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? TemplateType { get; set; }
    public string? FileName { get; set; }
}