namespace FurkanTural_Application.DTOs.Project;

/// <summary>
/// Portfolyo projesi. TechStack bir liste değil, virgülle ayrılmış etiketleri tek metinde taşır ve
/// bölme işi görünüm katmanına bırakılır. Description Markdown'dır, ShortDescription ise liste
/// kartlarındaki düz metindir.
/// </summary>
public class ProjectDto
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