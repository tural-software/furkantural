namespace FurkanTural_Application.DTOs.Newsletter;

public class NewsletterIssueDto
{
    public int Id { get; set; }
    public string? Subject { get; set; }
    public string? Status { get; set; }
    public DateTime? QueuedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
