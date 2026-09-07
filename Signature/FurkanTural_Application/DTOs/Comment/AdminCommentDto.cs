namespace FurkanTural_Application.DTOs.Comment;

/// <summary>Yönetim ekranındaki yorum satırı. Okura giden <see cref="CommentDto"/>'dan ayrıldığı yer adrestir: denetim kararını verebilmek için adresi görmek gerekir, çünkü aynı adresten gelen ikinci bir spam'i ancak öyle tanıyabilirsiniz.<para>Yazı başlığı ile üst yorumun sahibi satıra kopyalanır. İkisi de birleştirmeyle gelir; olmasalardı ekranı çizen taraf yorum başına iki sorgu daha açmak zorunda kalırdı.</para></summary>
public class AdminCommentDto
{
    public int Id { get; set; }
    public int BlogId { get; set; }
    public string? BlogTitle { get; set; }
    public string? BlogSlug { get; set; }

    public int? ParentId { get; set; }

    /// <summary>Yanıtlanan yorumun sahibi. Boşsa satır bir kök yorumdur.</summary>
    public string? ParentAuthorName { get; set; }

    public string? AuthorName { get; set; }
    public string? AuthorEmail { get; set; }
    public string? Body { get; set; }

    public string? Status { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public bool NotifyOnReply { get; set; }

    /// <summary>Bu yoruma verilmiş yanıt sayısı; silinmiş ve reddedilmiş olanlar dâhil. Silme kararında görünür durur: yanıtı olan bir yorumu kaldırmak, altındaki konuşmayı bağlamsız bırakır.</summary>
    public int ReplyCount { get; set; }

    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
    public int? DeletedBy { get; set; }
}

/// <summary>Denetim kuyruğunun durum sayaçları. Yönetim listesinin aktif/pasif/silinmiş sayaçlarından ayrıdır ve onların yerine geçmez: bunlar yorumun kendi denetim çizgisini sayar.</summary>
public class CommentModerationCountsDto
{
    public int Pending { get; set; }
    public int Approved { get; set; }
    public int Rejected { get; set; }
}
