using FurkanTural_Application.DTOs.Comment;
using FurkanTural_Domain.Entities;

namespace FurkanTural_Business.Mappers;

public static class CommentMapper
{
    /// <summary>Okura giden biçim. <see cref="Comment.AuthorEmail"/> <b>bilinçli olarak kopyalanmaz</b>; bu satırın eksikliği değil, adresin dışarı çıkmamasının tek güvencesidir.<para>Yazarlık damgası <c>CreatedBy</c>'dan okunur: ziyaretçi yorumunun oluşturanı yoktur, panelden verilen yanıt ise yöneticinin kimliğini taşır.</para></summary>
    public static CommentDto ToDto(this Comment entity) => new()
    {
        Id = entity.Id,
        BlogId = entity.BlogId,
        ParentId = entity.ParentId,
        AuthorName = entity.AuthorName,
        Body = entity.Body,
        CreatedAt = entity.CreatedAt,
        IsAuthor = entity.CreatedBy is not null
    };

    public static AdminCommentDto ToAdminDto(this Comment entity, string? blogTitle = null, string? blogSlug = null, string? parentAuthorName = null, int replyCount = 0) => new()
    {
        Id = entity.Id,
        BlogId = entity.BlogId,
        BlogTitle = blogTitle,
        BlogSlug = blogSlug,
        ParentId = entity.ParentId,
        ParentAuthorName = parentAuthorName,
        AuthorName = entity.AuthorName,
        AuthorEmail = entity.AuthorEmail,
        Body = entity.Body,
        Status = entity.Status,
        ApprovedAt = entity.ApprovedAt,
        NotifyOnReply = entity.NotifyOnReply,
        ReplyCount = replyCount,
        IsActive = entity.IsActive,
        IsDeleted = entity.IsDeleted,
        CreatedAt = entity.CreatedAt,
        CreatedBy = entity.CreatedBy,
        UpdatedAt = entity.UpdatedAt,
        UpdatedBy = entity.UpdatedBy,
        DeletedAt = entity.DeletedAt,
        DeletedBy = entity.DeletedBy
    };
}
