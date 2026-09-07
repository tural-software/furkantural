namespace FurkanTural_Application.DTOs.Comment;

/// <summary>Ziyaretçinin bıraktığı yorum. Alanların hiçbiri doğrulanmış bir kimlik değildir; ad ile adres yazanın beyanıdır.<para><see cref="ParentId"/> yayındaki herhangi bir yorumu gösterebilir; gösterdiği satırın kendisi de bir yanıt olabilir. Aranan tek şey üst yorumun aynı yazıya ait ve onaylanmış olmasıdır — onaysız bir yoruma yanıt verilebilseydi, form o yorumun varlığını ele veren bir araca dönüşürdü.</para></summary>
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
