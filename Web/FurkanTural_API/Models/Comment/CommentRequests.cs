namespace FurkanTural_API.Models.Comment;

/// <summary>Ziyaretçinin bıraktığı yorum. IP adresi gövdeden değil bağlantıdan okunur: istemcinin bildirdiği bir adres, bot doğrulamasının sorduğu soruyu anlamsız kılardı.</summary>
public class SubmitCommentRequest
{
    public int BlogId { get; set; }
    public int? ParentId { get; set; }
    public string? AuthorName { get; set; }
    public string? AuthorEmail { get; set; }
    public string? Body { get; set; }
    public bool NotifyOnReply { get; set; }
    public string? TurnstileToken { get; set; }
}

/// <summary>Yanıt bildirimlerini kapatan jeton. Postadaki bağlantıdan gelir.</summary>
public class CommentTokenRequest
{
    public string? Token { get; set; }
}

/// <summary>Yorumun denetim kararı: <c>Pending</c>, <c>Approved</c> ya da <c>Rejected</c>.</summary>
public class CommentStatusRequest
{
    public string? Status { get; set; }
}

/// <summary>Yazının sahibi olarak verilen yanıt. Yanıtlanan yorum adres satırından gelir, gövde buradan.</summary>
public class CommentReplyRequest
{
    public string? Body { get; set; }
}
