using FurkanTural_Chat;
using FurkanTural_Chat.Models.Auth;
using FurkanTural_Chat.Services;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_Chat.Controllers;

public class AccountController(IChatAuthApiClient authApiClient, IAppConfigService appConfigService) : Controller
{
    private readonly IChatAuthApiClient _authApiClient = authApiClient;
    private readonly IAppConfigService _appConfigService = appConfigService;

    [HttpGet]
    public async Task<IActionResult> Login(CancellationToken cancellationToken)
    {
        if (IsAuthenticated())
            return RedirectToAction("Index", "Chat");

        ViewBag.TurnstileSiteKey = await _appConfigService.GetTurnstileSiteKeyAsync(cancellationToken);
        return View(new LoginRequestModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginRequestModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return Json(new { ok = false, errors = ModelErrors() });

        var result = await _authApiClient.LoginAsync(model, cancellationToken);
        if (!result.Success || result.Data?.Token is null)
            return Json(new { ok = false, errors = ApiErrors(result.Errors, result.Message, "Giriş başarısız.") });

        StoreSession(result.Data);
        SetFlash("success", "Hoş geldin", result.Data.Username ?? string.Empty);
        return Json(new { ok = true, redirect = Url.Action("Index", "Chat") });
    }

    [HttpGet]
    public async Task<IActionResult> Register(CancellationToken cancellationToken)
    {
        if (IsAuthenticated())
            return RedirectToAction("Index", "Chat");

        ViewBag.TurnstileSiteKey = await _appConfigService.GetTurnstileSiteKeyAsync(cancellationToken);
        return View(new RegisterRequestModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterRequestModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return Json(new { ok = false, errors = ModelErrors() });

        var result = await _authApiClient.RegisterAsync(model, cancellationToken);
        if (!result.Success || result.Data?.Token is null)
            return Json(new { ok = false, errors = ApiErrors(result.Errors, result.Message, "Kayıt başarısız.") });

        StoreSession(result.Data);
        SetFlash("success", "Aramıza hoş geldin", result.Data.Username ?? string.Empty);
        return Json(new { ok = true, redirect = Url.Action("Index", "Chat") });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        SetFlash("success", "Çıkış yapıldı", "Tekrar görüşmek üzere.");
        return RedirectToAction(nameof(Login));
    }

    private bool IsAuthenticated()
        => !string.IsNullOrEmpty(HttpContext.Session.GetString("token"));

    private void StoreSession(AuthResultModel data)
    {
        HttpContext.Session.SetString("token", data.Token ?? string.Empty);
        HttpContext.Session.SetInt32("userId", data.UserId);
        HttpContext.Session.SetString("username", data.Username ?? string.Empty);
        HttpContext.Session.SetString("role", data.RoleName ?? string.Empty);
        HttpContext.Session.SetString("avatarUrl", data.AvatarUrl ?? string.Empty);
        HttpContext.Session.SetString("expiresAt", data.ExpiresAt.ToString("O"));
    }

    private void SetFlash(string type, string title, string msg)
    {
        TempData["ToastType"] = type;
        TempData["ToastTitle"] = title;
        TempData["ToastMsg"] = msg;
    }

    private List<string> ModelErrors()
        => ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();

    private static List<string> ApiErrors(List<string> errors, string? message, string fallback)
        => errors.Count > 0 ? errors : [!string.IsNullOrWhiteSpace(message) ? message : fallback];
}
