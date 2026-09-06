namespace FurkanTural_Application.DTOs.Newsletter;

/// <summary>Dağıtımın anlık durumu. <see cref="RecipientCount"/> liste dondurulduğunda sabitlenir ve payda olarak kullanılır; diğer dördü her okumada dağıtım satırlarından sayılır, dolayısıyla toplamları paydayı aşamaz ve ilerleme çubuğu geri gitmez.</summary>
public class NewsletterIssueProgressDto
{
    public int Id { get; set; }
    public string? Status { get; set; }
    public int RecipientCount { get; set; }
    public int SentCount { get; set; }
    public int FailedCount { get; set; }
    public int SkippedCount { get; set; }
    public int PendingCount { get; set; }
    public DateTime? QueuedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
