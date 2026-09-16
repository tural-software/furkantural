using FurkanTural_Admin.Models.DataRetention;
using FurkanTural_Admin.Services;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_Admin.Controllers;

/// <summary>Aylık saklama temizliği. Sayfa önce neyin silineceğini gösterir; silme ancak yönetici geri alınamaz olduğunu onaylayan kutuyu işaretleyip gönderdiğinde yapılır. Onay sunucuda denetlenir: kutu yalnızca tarayıcıda zorunlu olsaydı elle kurulan bir istek onu atlayabilirdi.</summary>
public class DataRetentionController(IDataRetentionApiClient dataRetentionApiClient) : Controller
{
    private readonly IDataRetentionApiClient _dataRetentionApiClient = dataRetentionApiClient;

    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login", "Auth");

        var preview = await _dataRetentionApiClient.PreviewAsync(token, cancellationToken);
        return View(new DataRetentionViewModel
        {
            Preview = preview,
            Error = preview is null ? "Saklama durumu alınamadı. Kısa süre sonra tekrar deneyin." : null
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Purge(bool confirm, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login", "Auth");

        if (!confirm)
        {
            var preview = await _dataRetentionApiClient.PreviewAsync(token, cancellationToken);
            return View("Index", new DataRetentionViewModel
            {
                Preview = preview,
                Error = "Silme işlemi geri alınamaz. Devam etmek için onay kutusunu işaretleyin."
            });
        }

        var purged = await _dataRetentionApiClient.PurgeAsync(token, cancellationToken);
        return View("Index", new DataRetentionViewModel
        {
            Purged = purged,
            Preview = await _dataRetentionApiClient.PreviewAsync(token, cancellationToken),
            Error = purged is null ? "Silme işlemi tamamlanamadı; hiçbir kayıt silinmedi." : null
        });
    }
}
