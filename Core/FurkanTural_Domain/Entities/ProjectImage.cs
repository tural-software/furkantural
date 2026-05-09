using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Domain.Entities;

public class ProjectImage : BaseEntity
{
    public string? Url { get; set; }
    public string? AltText { get; set; }
    public bool IsCover { get; set; }
    public int ProjectId { get; set; }
}
