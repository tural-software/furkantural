using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Domain.Entities;

/// <summary>
/// İki kullanıcı arası sesli/görüntülü arama kaydı (denetim + geçmiş).
/// Yaşam döngüsü: Ringing → Answered → Ended | Rejected | Missed | Canceled | Failed.
/// </summary>
public class CallLog : BaseEntity
{
    public int CallerId { get; set; }
    public int CalleeId { get; set; }
    /// <summary>"Audio" | "Video".</summary>
    public string? CallType { get; set; }
    /// <summary>Ringing/Answered/Ended/Rejected/Missed/Canceled/Failed.</summary>
    public string? Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? AnsweredAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int? DurationSeconds { get; set; }
}
