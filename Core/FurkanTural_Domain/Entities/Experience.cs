using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Domain.Entities;

/// <summary>Portfolyodaki iş deneyimi kaydı; EndDate null ise görev sürüyor kabul edilip öyle gösterilir.</summary>
public class Experience : BaseEntity
{
    public string? Position { get; set; }
    public string? CompanyName { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
