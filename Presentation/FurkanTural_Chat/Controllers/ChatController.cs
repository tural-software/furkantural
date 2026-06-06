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
            Token = token,
            ApiBaseUrl = _apiOptions.BaseUrl.TrimEnd('/'),
            UserId = HttpContext.Session.GetInt32("userId") ?? 0,
            Username = username,
            DisplayName = username,
            AvatarUrl = HttpContext.Session.GetString("avatarUrl") ?? string.Empty
        };

        return View(model);
    }
}
