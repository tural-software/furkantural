namespace FurkanTural_Application.DTOs.ProjectImage;

/// <summary>
/// Projenin görseli. IsCover tekil değildir: yeni bir görseli kapak işaretlemek eskisinin işaretini
/// kaldırmaz, hiçbir yerde teklik denetimi yapılmaz.
/// </summary>
public class ProjectImageDto
{
    public int Id { get; set; }
    public string? Url { get; set; }
    public string? AltText { get; set; }
    public bool IsCover { get; set; }
    public int ProjectId { get; set; }
}