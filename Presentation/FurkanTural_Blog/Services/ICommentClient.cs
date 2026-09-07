using FurkanTural_Blog.Models;

namespace FurkanTural_Blog.Services;

/// <summary>Yorum uçlarını uygulama jetonuyla çağırır; yetki kurulumu <see cref="INewsletterClient"/> ile aynıdır — uçlar <c>VisitorOrAbove</c> ister ve uygulama jetonu Visitor rolü taşır, ziyaretçi oturum açmaz.<para>Okuma hiçbir zaman adres taşımaz: uç onu döndürmez.</para></summary>
public interface ICommentClient
{
    /// <summary>Yazının yayındaki yorumlarını getirir. Arıza hâlinde boş bir bölüm döner: yorumların gelmemesi yazının kendisini gizlemek için sebep değildir.</summary>
    Task<CommentThreadViewModel> GetThreadAsync(int blogId, CancellationToken ct = default);

    /// <summary>Yorumu gönderir. Yanıt, kaydın gerçekten açılıp açılmadığını ele vermez.</summary>
    Task<CommentOutcome> SubmitAsync(CommentFormModel form, CancellationToken ct = default);

    /// <summary>Yanıt bildirimlerini kapatır.</summary>
    Task<CommentOutcome> DisableNotificationsAsync(string token, CancellationToken ct = default);
}

/// <summary>Yazma uçlarının sonucu. Metin doluysa API'nin kendi cümlesidir ve olduğu gibi gösterilir; boşsa çağıran kendi varsayılanını koyar.</summary>
public readonly record struct CommentOutcome(bool Succeeded, string? Message);
