using System.Security.Claims;
using Asp.Versioning;
using FurkanTural_Application.DTOs.Auth;
using FurkanTural_Application.DTOs.User;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_API.Controllers.Base;
using FurkanTural_API.Models.Auth;
using FurkanTural_Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

namespace FurkanTural_API.Controllers;

[ApiVersion("1.0")]
public class AuthController(IAuthService authService, IAccountActivationService accountActivationService) : BaseApiController
{
    private readonly IAuthService _authService = authService;
    private readonly IAccountActivationService _accountActivationService = accountActivationService;

    private const string AppTokenRole = "Visitor";

    /// <summary>Kullanıcı girişi yap ve JWT token al</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
        => ToActionResult(await _authService.LoginAsync(new LoginDto
        {
            Username = request.Username,
            Password = request.Password,
            AppSource = request.AppSource,
            TurnstileToken = request.TurnstileToken
        }, ClientIp(), ClientAgent(), TrustedAppSource(), cancellationToken));

    /// <summary>Uygulama varsayılan token'ı al (Visitor rolü, uzun süreli)</summary>
    [HttpPost("app-token")]
    [AllowAnonymous]
    public async Task<IActionResult> AppToken([FromBody] AppTokenRequest request, CancellationToken cancellationToken)
        => ToActionResult(await _authService.GenerateAppTokenAsync(new AppTokenRequestDto
        {
            AppKey = request.AppKey,
            AppName = request.AppName
        }, cancellationToken));

    /// <summary>Geçerli kullanıcı token'ını aynı kimlik ve app_source ile yenile (BFF oturumu sürerken kısa ömürlü JWT yüzünden kullanıcı düşmesin)</summary>
    [HttpPost("refresh")]
    [Authorize(Policy = "UserOrAdmin")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!int.TryParse(sub, out var userId))
            return Unauthorized();

        var appSource = User.FindFirst(ClaimDefinitions.AppSource)?.Value;
        var securityStamp = User.FindFirst(ClaimDefinitions.SecurityStamp)?.Value;
        var authTimeClaim = User.FindFirst(ClaimDefinitions.AuthTime) ?? User.FindFirst(ClaimTypes.AuthenticationInstant);
        DateTime? authTime = long.TryParse(authTimeClaim?.Value, out var seconds)
            ? DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime
            : null;

        return ToActionResult(await _authService.RefreshAsync(userId, appSource, securityStamp, authTime, cancellationToken));
    }

    /// <summary>Yeni üye kaydı oluştur ve giriş token'ı al</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
        => ToActionResult(await _authService.RegisterAsync(new RegisterDto
        {
            Username = request.Username,
            Email = request.Email,
            Password = request.Password,
            DisplayName = request.DisplayName,
            TurnstileToken = request.TurnstileToken,
            AcceptAgreement = request.AcceptAgreement,
            ConfirmAdult = request.ConfirmAdult
        }, ClientIp(), ClientAgent(), cancellationToken));

    /// <summary>Doğrulama bağlantısındaki jeton ile pasif hesabı yeniden etkinleştir</summary>
    [HttpPost("activate")]
    [AllowAnonymous]
    public async Task<IActionResult> Activate([FromBody] ActivateAccountRequest request, CancellationToken cancellationToken)
        => ToActionResult(await _accountActivationService.ConsumeAsync(request.Token, cancellationToken));

    /// <summary>İsteği yapan ön-yüzün kimliği. Yalnızca uygulama jetonundan okunur: o jeton <c>AppTokens:Apps</c> altındaki anahtarla üretilir ve her zaman Visitor rolü taşır, dolayısıyla değeri istemci uyduramaz. Gövdedeki AppSource bu karara hiç girmez — girseydi robot doğrulamasından kaçmak için alanı boş göndermek yeterdi.</summary>
    private string? TrustedAppSource()
        => User.Identity?.IsAuthenticated == true && User.IsInRole(AppTokenRole)
            ? User.FindFirst(ClaimDefinitions.AppSource)?.Value
            : null;

    /// <summary>Adres <c>UseRealClientIp</c> middleware'inden sonra okunur, dolayısıyla Cloudflare kenar adresini değil ziyaretçinin kendi adresini verir.</summary>
    private string? ClientIp()
        => HttpContext.Connection.RemoteIpAddress?.ToString();

    private string? ClientAgent()
        => HttpContext.Request.Headers.UserAgent.ToString();
}
