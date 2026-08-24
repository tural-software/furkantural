namespace FurkanTural_Application.DTOs.MailTemplateType;

/// <summary>Posta türünün liste görünümü. Placeholders boş dönüyorsa tür panelden eklenmiş demektir: şablonu saklanır ama onu dolduran bir gövde DTO'su ve gönderen bir kod yolu yoktur.</summary>
public class MailTemplateTypeDto
{
    public int Id { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public IReadOnlyList<string> Placeholders { get; set; } = [];
}
