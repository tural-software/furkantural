using FurkanTural_Blog.Models;
using FurkanTural_Blog.Services;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_Blog.Controllers;

/// <summary>Bülten sayfaları. Abonelik çift onaylıdır: form adresi listeye yazmaz, doğrulama bağlantısı gönderir ve liste ancak o bağlantıya tıklanınca büyür.<para>Çıkış da aynı yoldan gider. Adres tek başına yeterli olsaydı herhangi biri başkasının aboneliğini iptal edebilirdi; bu yüzden form yalnızca bağlantı ister, listeden düşüren adım jetonla çalışır.</para><para>Form JavaScript'siz de gönderilebilir ama Turnstile widget'ı JavaScript ister; jeton hiç gelmezse istek ağa çıkmadan elenir ve ziyaretçiye bot doğrulamasının tamamlanmadığı söylenir.</para><para>Sayfaların hiçbiri dizine girmez. Kayıt ve çıkış ekranları arama sonucunda görünmesi gereken içerik değildir.</para></summary>
public class NewsletterController(INewsletterClient newsletter, IAppConfigService appConfig) : Controller
{
    private readonly INewsletterClient _newsletter = newsletter;
    private readonly IAppConfigService _appConfig = appConfig;

    [HttpGet]
    [Route("bulten", Name = "BlogNewsletter")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        await AttachSiteKeyAsync(cancellationToken);
        return View(new NewsletterViewModel());
    }

    /// <summary>Tuzak alan doluysa istek API'ye hiç çıkmaz ama ekranda başarı görünür: bota tuzağa düştüğünü söylemek, tuzağı bir sonraki denemede işe yaramaz hâle getirir.<para>Gönderimden sonra yönlendirme yapılmaz. Sonuç metni tek kullanımlık olduğu için oturuma yazmak gerekirdi; sayfa zaten dizine girmediğinden yeniden gönderim uyarısının maliyeti, oturum taşımanın maliyetinden düşük.</para></summary>
    [HttpPost]
    [Route("bulten")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(NewsletterViewModel model, CancellationToken cancellationToken)
    {
        await AttachSiteKeyAsync(cancellationToken);
        model.Submitted = true;

        if (!string.IsNullOrWhiteSpace(model.Website))
        {
            model.Succeeded = true;
            model.ResultMessage = "Adresinize bir doğrulama bağlantısı gönderdik.";
            model.Email = null;
            return View(model);
        }

        if (!ModelState.IsValid)
        {
            model.Succeeded = false;
            return View(model);
        }

        if (string.IsNullOrWhiteSpace(model.TurnstileToken))
        {
            model.Succeeded = false;
            model.ResultMessage = "Bot doğrulaması tamamlanmadı. Lütfen tekrar deneyin.";
            return View(model);
        }

        var outcome = await _newsletter.SubscribeAsync(model.Email!.Trim(), model.TurnstileToken, cancellationToken);
        model.Succeeded = outcome.Succeeded;
        model.ResultMessage = string.IsNullOrWhiteSpace(outcome.Message)
            ? (outcome.Succeeded
                ? "Adresinize bir doğrulama bağlantısı gönderdik."
                : "Kayıt şu anda alınamıyor. Kısa süre sonra tekrar deneyin.")
            : outcome.Message;

        if (outcome.Succeeded)
            model.Email = null;

        return View(model);
    }

    /// <summary>Doğrulama bağlantısının indiği sayfa. Sayfayı açmak jetonu harcamaz, yalnızca onay düğmesini gösterir; jeton düğmeye basılınca harcanır. Posta sunucularının güvenlik tarayıcıları bağlantıları kendiliğinden açar — açmak işlemi yapsaydı abonelik okur hiç tıklamadan onaylanmış olurdu.</summary>
    [HttpGet]
    [Route("bulten/onay", Name = "BlogNewsletterConfirm")]
    public Task<IActionResult> Confirm(string? token, CancellationToken cancellationToken)
        => Task.FromResult<IActionResult>(View("Result", Pending(token, NewsletterTokenViewModel.ConfirmAction)));

    [HttpPost]
    [Route("bulten/onay", Name = "BlogNewsletterConfirmPost")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> ConfirmPost(string? token, CancellationToken cancellationToken)
        => ResolveAsync(token, t => _newsletter.ConfirmAsync(t, cancellationToken), "Onay");

    /// <summary>Çıkış bağlantısının indiği sayfa. Jeton yoksa sonuç değil form gösterilir: bağlantısız gelen ziyaretçi adresini yazıp bağlantıyı isteyebilir. Jeton varsa sayfa yalnızca çıkış düğmesini gösterir; çıkış düğmeye basılınca yapılır, aksi hâlde posta güvenlik tarayıcısı bağlantıyı açtığı anda okur listeden düşerdi.</summary>
    [HttpGet]
    [Route("bulten/cikis", Name = "BlogNewsletterUnsubscribe")]
    public Task<IActionResult> Unsubscribe(string? token, CancellationToken cancellationToken)
        => Task.FromResult<IActionResult>(string.IsNullOrWhiteSpace(token)
            ? View("Unsubscribe", new NewsletterViewModel())
            : View("Result", Pending(token, NewsletterTokenViewModel.UnsubscribeAction)));

    [HttpPost]
    [Route("bulten/cikis/onay", Name = "BlogNewsletterUnsubscribeConfirm")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> UnsubscribeConfirm(string? token, CancellationToken cancellationToken)
        => ResolveAsync(token, t => _newsletter.UnsubscribeAsync(t, cancellationToken), "Çıkış");

    /// <summary>Çıkış bağlantısını ister. Listeden düşürmez ve yanıt adresin listede olup olmadığını ele vermez.</summary>
    [HttpPost]
    [Route("bulten/cikis")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unsubscribe(NewsletterViewModel model, CancellationToken cancellationToken)
    {
        model.Submitted = true;

        if (!ModelState.IsValid)
        {
            model.Succeeded = false;
            return View("Unsubscribe", model);
        }

        var outcome = await _newsletter.RequestUnsubscribeAsync(model.Email!.Trim(), cancellationToken);
        model.Succeeded = outcome.Succeeded;
        model.ResultMessage = string.IsNullOrWhiteSpace(outcome.Message)
            ? (outcome.Succeeded
                ? "Adres listemizdeyse çıkış bağlantısını gönderdik."
                : "İstek şu anda alınamıyor. Kısa süre sonra tekrar deneyin.")
            : outcome.Message;

        if (outcome.Succeeded)
            model.Email = null;

        return View("Unsubscribe", model);
    }

    private static NewsletterTokenViewModel Pending(string? token, string action)
        => string.IsNullOrWhiteSpace(token)
            ? new NewsletterTokenViewModel { TokenMissing = true }
            : new NewsletterTokenViewModel { Token = token.Trim(), PendingAction = action };

    private async Task<IActionResult> ResolveAsync(string? token, Func<string, Task<NewsletterOutcome>> action, string kind)
    {
        if (string.IsNullOrWhiteSpace(token))
            return View("Result", new NewsletterTokenViewModel { TokenMissing = true });

        var outcome = await action(token.Trim());
        return View("Result", new NewsletterTokenViewModel
        {
            Succeeded = outcome.Succeeded,
            Message = string.IsNullOrWhiteSpace(outcome.Message)
                ? (outcome.Succeeded ? $"{kind} tamamlandı." : "Bu bağlantı artık geçerli değil.")
                : outcome.Message
        });
    }

    private async Task AttachSiteKeyAsync(CancellationToken cancellationToken)
        => ViewBag.TurnstileSiteKey = await _appConfig.GetTurnstileSiteKeyAsync(cancellationToken);
}
