using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Domain.Entities;

/// <summary>
/// Kullanıcı/mesaj/medya/arama şikayeti. Admin tarafından incelenir.
/// </summary>
public class Report : BaseEntity
{
    public int ReporterId { get; set; }
    /// <summary>Şikayet edilen kullanıcı (varsa).</summary>
    public int? ReportedUserId { get; set; }
    /// <summary>"User" | "Message" | "Media" | "Call".</summary>
    public string? TargetType { get; set; }
    /// <summary>Hedef kaydın Id'si (ör. ChatMessage.Id / CallLog.Id); kullanıcı şikayetinde null olabilir.</summary>
    public int? TargetId { get; set; }
    public string? Reason { get; set; }
    /// <summary>Pending/Reviewed/Dismissed/ActionTaken.</summary>
    public string? Status { get; set; }
    public string? AdminNote { get; set; }
}
