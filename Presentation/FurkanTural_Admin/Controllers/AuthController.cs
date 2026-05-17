using FurkanTural_Admin.Models.Auth;
using FurkanTural_Admin.Services;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_Admin.Controllers;

public class AuthController(IAuthApiClient authApiClient) : Controller
{
    private readonly IAuthApiClient _authApiClient = authApiClient;

    [HttpGet]
    public IActionResult Login()
    {
        if (!string.IsNullOrEmpty(HttpContext.Session.GetString("token")))
            return RedirectToAction("Index", "Dashboard");

        return View(new LoginRequestModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login([FromForm] LoginRequestModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            return Json(new { ok = false, errors });
        }

        var result = await _authApiClient.LoginAsync(model, cancellationToken);

        if (!result.Success || result.Data is null || string.IsNullOrEmpty(result.Data.Token))
        {
            var errors = result.Errors.Count > 0
                ? result.Errors
                : new List<string> { result.Message ?? "Giriş başarısız." };
            return Json(new { ok = false, errors });
        }

        if (result.Data.RoleName != "Admin")
            return Json(new { ok = false, errors = new List<string> { "Bu panele erişim yalnızca yöneticilere açıktır." } });

        HttpContext.Session.SetString("token", result.Data.Token);
        HttpContext.Session.SetString("username", result.Data.Username ?? model.Username);
        HttpContext.Session.SetString("role", result.Data.RoleName ?? string.Empty);
        HttpContext.Session.SetString("expiresAt", result.Data.ExpiresAt.ToString("O"));

        return Json(new { ok = true, redirect = Url.Action("Index", "Dashboard") });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction(nameof(Login));
    }
}
