namespace FurkanTural_API.Models.Project;

public class CreateProjectRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? ShortDescription { get; set; }
    public string? TechStack { get; set; }
    public string? GitHubUrl { get; set; }
    public string? DemoUrl { get; set; }
    public bool IsCompleted { get; set; }
}

public class UpdateProjectRequest
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? ShortDescription { get; set; }
    public string? TechStack { get; set; }
    public string? GitHubUrl { get; set; }
    public string? DemoUrl { get; set; }
    public bool IsCompleted { get; set; }
}