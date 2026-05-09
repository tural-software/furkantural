namespace FurkanTural_Application.DTOs.ProjectImage;

public class UpdateProjectImageDto
{
    public int Id { get; set; }
    public string? Url { get; set; }
    public string? AltText { get; set; }
    public bool IsCover { get; set; }
    public int ProjectId { get; set; }
    public int? UpdatedBy { get; set; }
}
