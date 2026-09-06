namespace FurkanTural_Application.DTOs.Newsletter;

/// <summary>Yönetim görünümü; gövdeyi ve dağıtım sayaçlarını taşır. Sayaçlar dağıtım satırlarından yeniden sayılarak tazelenir, dolayısıyla listede görünen rakam ile ilerleme ekranındaki rakam aynı kaynaktan gelir.</summary>
public class AdminNewsletterIssueDto
{
    public int Id { get; set; }
    public string? Subject { get; set; }
    public string? Body { get; set; }
    public string? Status { get; set; }
    public DateTime? QueuedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int RecipientCount { get; set; }
    public int SentCount { get; set; }
    public int FailedCount { get; set; }
    public int SkippedCount { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
    public int? DeletedBy { get; set; }
}
