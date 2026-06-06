using Asp.Versioning;
using FurkanTural_Application.DTOs.Auth;
using FurkanTural_Application.DTOs.User;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_API.Controllers.Base;
using FurkanTural_API.Models.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_API.Controllers;

[ApiVersion("1.0")]
public class AuthController(IAuthService authService) : BaseApiController
{
    private readonly IAuthService _authService = authService;

    /// <summary>
    /// Kullanıcı girişi yap ve JWT token al
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
        => ToActionResult(await _authService.LoginAsync(new LoginDto
        {
            Username = request.Username,
            Password = request.Password,
            AppSource = request.AppSource,
            TurnstileToken = request.TurnstileToken
        }, cancellationToken));

    /// <summary>
    /// Uygulama varsayılan token'ı al (Visitor rolü, uzun süreli)
    /// </summary>
    [HttpPost("app-token")]
    [AllowAnonymous]
    public async Task<IActionResult> AppToken([FromBody] AppTokenRequest request, CancellationToken cancellationToken)
        => ToActionResult(await _authService.GenerateAppTokenAsync(new AppTokenRequestDto
        {
            AppKey = request.AppKey,
            AppName = request.AppName
        }, cancellationToken));

    /// <summary>
    /// Yeni üye kaydı oluştur ve giriş token'ı al
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
        => ToActionResult(await _authService.RegisterAsync(new RegisterDto
        {
            Username = request.Username,
            Email = request.Email,
            Password = request.Password,
            DisplayName = request.DisplayName,
            TurnstileToken = request.TurnstileToken
        }, cancellationToken));
}