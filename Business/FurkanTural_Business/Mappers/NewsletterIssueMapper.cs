using FurkanTural_Application.DTOs.Newsletter;
using FurkanTural_Domain.Entities;

namespace FurkanTural_Business.Mappers;

public static class NewsletterIssueMapper
{
    public static NewsletterIssueDto ToDto(this NewsletterIssue entity) => new()
    {
        Id = entity.Id,
        Subject = entity.Subject,
        Status = entity.Status,
        QueuedAt = entity.QueuedAt,
        CompletedAt = entity.CompletedAt
    };

    public static AdminNewsletterIssueDto ToAdminDto(this NewsletterIssue entity) => new()
    {
        Id = entity.Id,
        Subject = entity.Subject,
        Body = entity.Body,
        Status = entity.Status,
        QueuedAt = entity.QueuedAt,
        CompletedAt = entity.CompletedAt,
        RecipientCount = entity.RecipientCount,
        SentCount = entity.SentCount,
        FailedCount = entity.FailedCount,
        SkippedCount = entity.SkippedCount,
        IsActive = entity.IsActive,
        IsDeleted = entity.IsDeleted,
        CreatedAt = entity.CreatedAt,
        CreatedBy = entity.CreatedBy,
        UpdatedAt = entity.UpdatedAt,
        UpdatedBy = entity.UpdatedBy,
        DeletedAt = entity.DeletedAt,
        DeletedBy = entity.DeletedBy
    };

    public static NewsletterIssueProgressDto ToProgressDto(this NewsletterIssue entity, int pendingCount) => new()
    {
        Id = entity.Id,
        Status = entity.Status,
        RecipientCount = entity.RecipientCount,
        SentCount = entity.SentCount,
        FailedCount = entity.FailedCount,
        SkippedCount = entity.SkippedCount,
        PendingCount = pendingCount,
        QueuedAt = entity.QueuedAt,
        CompletedAt = entity.CompletedAt
    };
}
