using System.Security.Claims;
using Asp.Versioning;
using FurkanTural_Application.DTOs.Auth;
using FurkanTural_Application.DTOs.User;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_API.Controllers.Base;
using FurkanTural_API.Models.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

namespace FurkanTural_API.Controllers;

[ApiVersion("1.0")]
public class AuthController(IAuthService authService, IAccountActivationService accountActivationService) : BaseApiController
{
    private readonly IAuthService _authService = authService;
    private readonly IAccountActivationService _accountActivationService = accountActivationService;

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
        }, ClientIp(), ClientAgent(), cancellationToken));

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

        var appSource = User.FindFirst("app_source")?.Value;
        return ToActionResult(await _authService.RefreshAsync(userId, appSource, cancellationToken));
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
            AcceptAgreement = request.AcceptAgreement
        }, ClientIp(), ClientAgent(), cancellationToken));

    /// <summary>Doğrulama bağlantısındaki jeton ile pasif hesabı yeniden etkinleştir</summary>
    [HttpPost("activate")]
    [AllowAnonymous]
    public async Task<IActionResult> Activate([FromBody] ActivateAccountRequest request, CancellationToken cancellationToken)
        => ToActionResult(await _accountActivationService.ConsumeAsync(request.Token, cancellationToken));

    /// <summary>Adres <c>UseRealClientIp</c> middleware'inden sonra okunur, dolayısıyla Cloudflare kenar adresini değil ziyaretçinin kendi adresini verir.</summary>
    private string? ClientIp()
        => HttpContext.Connection.RemoteIpAddress?.ToString();

    private string? ClientAgent()
        => HttpContext.Request.Headers.UserAgent.ToString();
}
