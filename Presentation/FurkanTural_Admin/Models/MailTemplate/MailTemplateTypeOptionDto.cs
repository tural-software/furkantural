namespace FurkanTural_Admin.Models.MailTemplate;

/// <summary>Şablon formundaki tür listesi. Placeholders boşsa tür panelden eklenmiş demektir: şablonu saklanır ama onu gönderen bir kod yolu yoktur.</summary>
public sealed class MailTemplateTypeOptionDto
{
    public int Id { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public IReadOnlyList<string> Placeholders { get; set; } = [];
}
