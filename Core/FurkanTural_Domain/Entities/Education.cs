using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Domain.Entities;

/// <summary>Portfolyodaki eğitim kaydı; EndDate null ise eğitim sürüyor kabul edilip öyle gösterilir.</summary>
public class Education : BaseEntity
{
    public string? Institution { get; set; }
    public string? Degree { get; set; }
    public string? FieldOfStudy { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
