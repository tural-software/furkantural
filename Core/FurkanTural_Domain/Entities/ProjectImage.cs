using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Domain.Entities;

/// <summary>
/// <see cref="Project"/> görseli (ProjectId). Url tam adres değil, wwwroot/images/uploads altındaki
/// dosya adıdır.
/// </summary>
public class ProjectImage : BaseEntity
{
    public string? Url { get; set; }
    public string? AltText { get; set; }
    public bool IsCover { get; set; }
    public int ProjectId { get; set; }
}
