using FurkanTural_Chat.Models.Chat;
using FurkanTural_Chat.Models.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FurkanTural_Chat.Controllers;

public class ChatController(IOptions<ApiOptions> apiOptions) : Controller
{
    private readonly ApiOptions _apiOptions = apiOptions.Value;

    public IActionResult Index()
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login", "Account");

        var username = HttpContext.Session.GetString("username") ?? string.Empty;

        var model = new ChatPageViewModel
        {
            ApiBaseUrl = _apiOptions.BaseUrl.TrimEnd('/'),
            UserId = HttpContext.Session.GetInt32("userId") ?? 0,
            Username = username,
            DisplayName = username,
            AvatarUrl = HttpContext.Session.GetString("avatarUrl") ?? string.Empty,
            AgreementAccepted = HttpContext.Session.GetString("agreementAccepted") == "1"
        };

        return View(model);
    }

    /// <summary>Üyelik sözleşmesi API üzerinden kabul edildikten sonra, sunum oturumunu da işaretler. Böylece sayfa yenilense bile zorunlu onay modalı tekrar açılmaz (oturum içi kalıcılık).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AgreementAccepted()
    {
        if (string.IsNullOrEmpty(HttpContext.Session.GetString("token")))
            return Unauthorized();

        HttpContext.Session.SetString("agreementAccepted", "1");
        return Ok();
    }
}
