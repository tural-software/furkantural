using FurkanTural_Blog.Models;
using FurkanTural_Blog.Services;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_Blog.Controllers;

/// <summary>Bülten kayıt sayfası. Form JavaScript'siz çalışır: gönderim sayfayı yeniden çizer, sonuç aynı ekranda görünür. Sitede jQuery ya da doğrulama kitaplığı yok; istemci tarafındaki tek denetim tarayıcının kendi <c>type="email" required</c> denetimidir ve sunucu doğrulaması onun yerine geçmez, arkasında durur.<para>Sayfa dizine girmez. Kayıt formu arama sonucunda görünmesi gereken bir içerik değildir ve bültenin kendi tanıtımı Hakkında sayfasında zaten var.</para></summary>
public class NewsletterController(INewsletterClient newsletter) : Controller
{
    private readonly INewsletterClient _newsletter = newsletter;

    [HttpGet]
    [Route("bulten", Name = "BlogNewsletter")]
    public IActionResult Index() => View(new NewsletterViewModel());

    /// <summary>Tuzak alan doluysa istek API'ye hiç çıkmaz ama ekranda başarı görünür: bota tuzağa düştüğünü söylemek, tuzağı bir sonraki denemede işe yaramaz hâle getirir.<para>Gönderimden sonra yönlendirme yapılmaz. Sonuç metni tek kullanımlık olduğu için oturuma yazmak gerekirdi; sayfa zaten dizine girmediğinden yeniden gönderim uyarısının maliyeti, oturum taşımanın maliyetinden düşük.</para></summary>
    [HttpPost]
    [Route("bulten")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(NewsletterViewModel model, CancellationToken cancellationToken)
    {
        model.Submitted = true;

        if (!string.IsNullOrWhiteSpace(model.Website))
        {
            model.Succeeded = true;
            model.ResultMessage = "Abonelik başarıyla tamamlandı.";
            model.Email = null;
            return View(model);
        }

        if (!ModelState.IsValid)
        {
            model.Succeeded = false;
            return View(model);
        }

        var outcome = await _newsletter.SubscribeAsync(model.Email!.Trim(), cancellationToken);
        model.Succeeded = outcome.Succeeded;
        model.ResultMessage = string.IsNullOrWhiteSpace(outcome.Message)
            ? (outcome.Succeeded ? "Abonelik başarıyla tamamlandı." : "Kayıt şu anda alınamıyor. Kısa süre sonra tekrar deneyin.")
            : outcome.Message;

        if (outcome.Succeeded)
            model.Email = null;

        return View(model);
    }
}
