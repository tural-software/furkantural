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

    /// <summary>Oturum, kimlik doğrulandıktan sonra ve yeni değerler yazılmadan önce boşaltılır. Böylece girişten önce oluşturulmuş bir oturum kimliği doğrulanmış kullanıcıyı taşıyamaz. Yalnızca içerik sıfırlanır; oturum ve çerez yapılandırmasına dokunulmaz.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginRequestModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return Json(new { ok = false, errors = ModelErrors() });

        var result = await _authApiClient.LoginAsync(model, cancellationToken);
        if (!result.Success || result.Data?.Token is null)
            return Json(new { ok = false, errors = ApiErrors(result.Errors, result.Message, "Giriş başarısız.") });

        HttpContext.Session.Clear();
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

    /// <summary>Giriş akışındaki gibi, yeni hesabın oturumu yazılmadan önce mevcut oturum boşaltılır.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterRequestModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return Json(new { ok = false, errors = ModelErrors() });

        var result = await _authApiClient.RegisterAsync(model, cancellationToken);
        if (!result.Success || result.Data?.Token is null)
            return Json(new { ok = false, errors = ApiErrors(result.Errors, result.Message, "Kayıt başarısız.") });

        HttpContext.Session.Clear();
        StoreSession(result.Data);
        SetFlash("success", "Aramıza hoş geldin", result.Data.Username ?? string.Empty);
        return Json(new { ok = true, redirect = Url.Action("Index", "Chat") });
    }

    /// <summary>Sayfayı açmak hesabı açmaz; jeton yalnızca onay gönderiminde harcanır (bkz. <see cref="ActivateAccountModel"/>).</summary>
    [HttpGet]
    public IActionResult Activate(string? token)
    {
        if (IsAuthenticated())
            return RedirectToAction("Index", "Chat");

        return View(new ActivateAccountModel
        {
            Token = token,
            State = string.IsNullOrWhiteSpace(token) ? ActivationState.MissingToken : ActivationState.Confirm
        });
    }

    /// <summary>Başarılı açılışta oturum kurulmaz. Jetonu elinde tutan kişi hesabın adresine erişebiliyor demektir, ama bu parolayı bildiğini göstermez; hesabı açmak ile hesaba girmek ayrı kalır.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(ActivateAccountModel model, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(model.Token))
        {
            model.State = ActivationState.MissingToken;
            return View(model);
        }

        var result = await _authApiClient.ActivateAsync(model.Token, cancellationToken);

        model.State = result.Success ? ActivationState.Success : ActivationState.Failed;
        model.Message = result.Success
            ? (!string.IsNullOrWhiteSpace(result.Message) ? result.Message : "Hesabınız yeniden etkinleştirildi.")
            : ApiErrors(result.Errors, result.Message, "Doğrulama bağlantısı geçersiz.")[0];

        return View(model);
    }

    [HttpGet]
    public IActionResult Close()
    {
        if (!IsAuthenticated())
            return RedirectToAction(nameof(Login));

        return View(new CloseAccountModel());
    }

    /// <summary>Oturum, API kapatmayı kabul ettikten sonra boşaltılır. Ters sırada yapılsaydı istek yetkisiz kalır, hesap açık kalır ve kullanıcı kapattığını sanırdı.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(CloseAccountModel model, CancellationToken cancellationToken)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return RedirectToAction(nameof(Login));

        if (!ModelState.IsValid)
            return View(model);

        var result = await _authApiClient.DeactivateAsync(token, model.Password, cancellationToken);
        if (!result.Success)
        {
            model.Error = ApiErrors(result.Errors, result.Message, "Hesap kapatılamadı.")[0];
            model.Password = null;
            return View(model);
        }

        HttpContext.Session.Clear();
        SetFlash("success", "Hesabınız kapatıldı",
            "Aynı bilgilerle giriş yapmayı denediğinizde adresinize yeniden açma bağlantısı gönderilir.");
        return RedirectToAction(nameof(Login));
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
        HttpContext.Session.SetString("agreementAccepted", data.MembershipAgreementAccepted ? "1" : "0");
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
