using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Domain.Entities;

/// <summary>
/// İki kullanıcı arasındaki arama denemesinin kaydı; CallerId ve CalleeId <see cref="User"/>'a
/// bakar. CallType <see cref="Constants.CallDefinitions.Types"/>, Status
/// <see cref="Constants.CallDefinitions.Statuses"/> sabitlerinden gelir ve şu akışı izler:
/// Ringing → Answered → Ended | Rejected | Missed | Canceled | Failed.
/// </summary>
public class CallLog : BaseEntity
{
    public int CallerId { get; set; }
    public int CalleeId { get; set; }
    public string? CallType { get; set; }
    public string? Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? AnsweredAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int? DurationSeconds { get; set; }
}
