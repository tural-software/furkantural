namespace FurkanTural_Application.DTOs.Comment;

/// <summary>Ziyaretçinin bıraktığı yorum. Alanların hiçbiri doğrulanmış bir kimlik değildir; ad ile adres yazanın beyanıdır.<para><see cref="ParentId"/> yalnızca kök bir yorumu gösterebilir. Yanıtın yanıtı reddedilir — sınır sunumsaldır, dar ekranda üçüncü seviyeden sonra metin sütunu okunmaz hâle gelir.</para></summary>
public class SubmitCommentDto
{
    public int BlogId { get; set; }
    public int? ParentId { get; set; }
    public string? AuthorName { get; set; }
    public string? AuthorEmail { get; set; }
    public string? Body { get; set; }

    /// <summary>Bu yoruma yanıt geldiğinde posta istenip istenmediği. Varsayılan kapalıdır: istenmemiş posta göndermemenin tek güvencesi, açık bir işaret aramaktır.</summary>
    public bool NotifyOnReply { get; set; }
}

/// <summary>Yazının sahibinin panelden verdiği yanıt. Ziyaretçi formundan üç yerde ayrılır: kimlik yapılandırmadan gelir, bot doğrulaması aranmaz ve satır beklemeden yayına girer — kendi sitesinde kendi yazdığını onaylamak için sıraya girmenin bir anlamı yok.</summary>
public class AdminReplyCommentDto
{
    public int ParentId { get; set; }
    public string? Body { get; set; }
}
