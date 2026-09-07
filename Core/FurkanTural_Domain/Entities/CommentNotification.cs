using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Domain.Entities;

/// <summary>Bir yanıtın, yanıtladığı yorumun sahibine bildirileceğinin kaydı. Onay anında açılır, postayı arka plandaki dağıtıcı gönderir.<para>Ayrı bir satır olmasının sebebi onayın hızlı bitmesi değil, kaybolmamasıdır: postayı onay isteğinin içinde göndermek, SMTP ayakta değilken yöneticiyi onaylayamaz hâle getirir; hatayı yutmak ise bildirimi sessizce düşürür. Satır durduğu sürece deneme hakkı da durur.</para><para>Bir yanıt için en fazla bir satır açılır (<see cref="CommentId"/> tekildir). Reddedilip yeniden onaylanan bir yanıt bu yüzden ikinci kez posta üretmez — okur için o iki onay tek bir olaydır.</para><para><see cref="TokenHash"/> yalnızca gönderim başarılıysa yazılır. Ters sıra, gönderilemeyen her denemede kullanılmayacak bir kimlik bilgisi biriktirirdi.</para></summary>
public class CommentNotification : BaseEntity
{
    /// <summary>Bildirimi doğuran yanıt. Alıcı bu yanıtın bağlı olduğu üst yorumun sahibidir.</summary>
    public int CommentId { get; set; }

    /// <summary>Alıcının adresi, kuyruğa girerken kopyalanır. Üst yorum sonradan silinse de gönderilmiş bir bildirimin kime gittiği kayıtta kalır.</summary>
    public string? Email { get; set; }

    public string? Status { get; set; }

    public int AttemptCount { get; set; }

    public DateTime? SentAt { get; set; }

    /// <summary>Son denemenin hata metni. Deneme hakkı tükendiğinde satırda kalır ve niçin gitmediğini söyleyen tek kayıt olur.</summary>
    public string? Error { get; set; }

    /// <summary>Postaya gömülen çıkış bağlantısının tuzsuz SHA-256 özeti. Jetonun düz hâli yalnızca giden postada bulunur; veri tabanını okuyan biri onu geri üretemez.</summary>
    public string? TokenHash { get; set; }
}
