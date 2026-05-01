using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Domain.Entities;

public class Education : BaseEntity
{
    public string? Institution { get; set; }
    public string? Degree { get; set; }
    public string? FieldOfStudy { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}