using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Domain.Entities;

/// <summary>Portfolyodaki proje kaydı; görsellerini <see cref="ProjectImage"/> taşır. TechStack ayrı kayıt değil, tek metinde virgülle ayrılmış etiket listesidir ve görünüm katmanı bu ayraçla böler.</summary>
public class Project : BaseEntity
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? ShortDescription { get; set; }
    public string? TechStack { get; set; }
    public string? GitHubUrl { get; set; }
    public string? DemoUrl { get; set; }
    public bool IsCompleted { get; set; }
}
