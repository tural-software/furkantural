using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Domain.Entities;

/// <summary>Kullanıcı/mesaj/medya/arama şikayeti; ReporterId ve ReportedUserId <see cref="User"/>'a bakar. TargetType ve Status <see cref="Constants.ReportDefinitions"/> sabitlerinden gelir. TargetId'nin foreign key'i yoktur: hangi tablonun kaydını gösterdiğini TargetType belirler.</summary>
public class Report : BaseEntity
{
    public int ReporterId { get; set; }
    public int? ReportedUserId { get; set; }
    public string? TargetType { get; set; }
    public int? TargetId { get; set; }
    public string? Reason { get; set; }
    public string? Status { get; set; }
    public string? AdminNote { get; set; }
}
