namespace FurkanTural_Admin.Models.NewsletterIssue;

/// <summary>API'nin bülten satırının panel kopyası. Alan adları API sözleşmesiyle birebir aynıdır; bir alanın adı değişirse burada sessizce boş kalır.</summary>
public class NewsletterIssueAdminDto
{
    public int Id { get; set; }
    public string? Subject { get; set; }
    public string? Body { get; set; }
    public string? Status { get; set; }
    public DateTime? QueuedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int RecipientCount { get; set; }
    public int SentCount { get; set; }
    public int FailedCount { get; set; }
    public int SkippedCount { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
    public int? DeletedBy { get; set; }
}

public class NewsletterIssueFormDto
{
    public string? Subject { get; set; }
    public string? Body { get; set; }
}

public class NewsletterIssueProgressModel
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

public class NewsletterIssueIndexViewModel
{
    public IReadOnlyList<NewsletterIssueAdminDto> Rows { get; set; } = [];
    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
    public int PassiveCount { get; set; }
    public int DeletedCount { get; set; }
    public int ConfirmedSubscriberCount { get; set; }
    public string? SearchSubject { get; set; }
    public string? ActiveFilter { get; set; }
    public string? DeletedFilter { get; set; }
    public string? DateFrom { get; set; }
    public string? DateTo { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalFiltered { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalFiltered / PageSize) : 0;
}
