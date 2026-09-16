using FurkanTural_Blog.Models;
using FurkanTural_Blog.Services;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_Blog.Controllers;

/// <summary>Yorum bildirimlerinin kapatıldığı sayfa. Yorumun kendisi burada değil <see cref="HomeController"/>'da gönderilir: başarısız bir gönderimde çizilmesi gereken şey yazının tüm sayfasıdır ve o sayfa oradan çizilir.<para>Jeton adres satırından gelir ve tek işi vardır — o adrese bir daha yanıt bildirimi göndermemek. Bültendeki çıkış jetonundan farklı olarak süresi yoktur: bildirim postası gelen kutusunda yıllarca durabilir ve o gün çalışmayan bir kapatma bağlantısı, adresin izinli kalmasını okurun sabrına bağlamak olurdu.</para><para>Bağlantıyı açmak bildirimleri kapatmaz, yalnızca düğmeyi gösterir; kapatma düğmeye basılınca yapılır. Posta güvenlik tarayıcıları bağlantıları kendiliğinden açar ve açmak işlemi yapsaydı okur hiç tıklamadan bildirimleri kapanmış olurdu.</para><para>Sayfa dizine girmez; kapatma ekranı arama sonucunda görünmesi gereken içerik değildir.</para></summary>
public class CommentController(ICommentClient comments) : Controller
{
    private readonly ICommentClient _comments = comments;

    [HttpGet]
    [Route("yorum/bildirim-kapat", Name = "BlogCommentNotificationOff")]
    public IActionResult NotificationOff(string? token)
        => View("Result", string.IsNullOrWhiteSpace(token)
            ? new CommentNotificationViewModel { TokenMissing = true }
            : new CommentNotificationViewModel { Token = token.Trim(), Pending = true });

    [HttpPost]
    [Route("yorum/bildirim-kapat", Name = "BlogCommentNotificationOffPost")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NotificationOffPost(string? token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
            return View("Result", new CommentNotificationViewModel { TokenMissing = true });

        var outcome = await _comments.DisableNotificationsAsync(token.Trim(), cancellationToken);

        return View("Result", new CommentNotificationViewModel
        {
            Succeeded = outcome.Succeeded,
            Message = string.IsNullOrWhiteSpace(outcome.Message)
                ? (outcome.Succeeded
                    ? "Yanıt bildirimleri kapatıldı."
                    : "Bu bağlantı artık geçerli değil.")
                : outcome.Message
        });
    }
}
