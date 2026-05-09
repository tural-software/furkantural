namespace FurkanTural_Application.DTOs.Project;

public class UpdateProjectDto
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? ShortDescription { get; set; }
    public string? TechStack { get; set; }
    public string? GitHubUrl { get; set; }
    public string? DemoUrl { get; set; }
    public bool IsCompleted { get; set; }
    public int? UpdatedBy { get; set; }
}
