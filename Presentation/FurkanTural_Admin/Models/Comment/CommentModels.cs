namespace FurkanTural_Admin.Models.Comment;

public sealed class CommentAdminDto
{
    public int Id { get; set; }
    public int BlogId { get; set; }
    public string? BlogTitle { get; set; }
    public string? BlogSlug { get; set; }

    public int? ParentId { get; set; }
    public string? ParentAuthorName { get; set; }

    public string? AuthorName { get; set; }

    /// <summary>Yorumu bırakanın adresi. Yalnızca panelde görünür; okura giden hiçbir yanıt bu alanı taşımaz.</summary>
    public string? AuthorEmail { get; set; }

    public string? Body { get; set; }

    public string? Status { get; set; }
    public DateTime? ApprovedAt { get; set; }

    /// <summary>Yoruma yanıt gelince adrese posta gidip gitmeyeceği. Yanıt yazmadan önce görünür durur: kapalıysa yazılan yanıt kimseye duyurulmaz.</summary>
    public bool NotifyOnReply { get; set; }

    public int ReplyCount { get; set; }

    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
    public int? DeletedBy { get; set; }

    public bool IsPending => Status == "Pending";
    public bool IsApproved => Status == "Approved";
    public bool IsRejected => Status == "Rejected";

    /// <summary>Panelden yazılan yanıt mı. Ziyaretçi yorumunun oluşturanı yoktur; yazının sahibinin yanıtı yöneticinin kimliğini taşır.</summary>
    public bool IsAuthorReply => CreatedBy is not null;

    public string StatusLabel => Status switch
    {
        "Approved" => "Onaylı",
        "Rejected" => "Reddedildi",
        "Pending" => "Bekliyor",
        _ => "—"
    };
}

public sealed class CommentModerationCounts
{
    public int Pending { get; set; }
    public int Approved { get; set; }
    public int Rejected { get; set; }
}

public sealed class CommentReplyFormDto
{
    public string? Body { get; set; }
}

public sealed class CommentIndexViewModel
{
    public IReadOnlyList<CommentAdminDto> Rows { get; init; } = [];

    public int TotalCount { get; init; }
    public int ActiveCount { get; init; }
    public int PassiveCount { get; init; }
    public int DeletedCount { get; init; }

    public int PendingCount { get; init; }
    public int ApprovedCount { get; init; }
    public int RejectedCount { get; init; }

    public string? SearchName { get; init; }
    public string? StatusFilter { get; init; }
    public string? ActiveFilter { get; init; }
    public string? DeletedFilter { get; init; }
    public string? DateFrom { get; init; }
    public string? DateTo { get; init; }

    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public int TotalFiltered { get; init; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalFiltered / PageSize) : 0;
}
