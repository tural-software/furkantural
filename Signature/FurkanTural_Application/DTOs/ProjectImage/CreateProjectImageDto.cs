namespace FurkanTural_Application.DTOs.ProjectImage;

public class CreateProjectImageDto
{
    public string? Url { get; set; }
    public string? AltText { get; set; }
    public bool IsCover { get; set; }
    public int ProjectId { get; set; }
    public int? CreatedBy { get; set; }
}