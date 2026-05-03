using Asp.Versioning;
using FurkanTural_Application.DTOs.Auth;
using FurkanTural_Application.DTOs.User;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_API.Controllers.Base;
using FurkanTural_API.Models.Auth;
using FurkanTural_API.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_API.Controllers;

[ApiVersion("1.0")]
public class AuthController(IAuthService authService, IUserService userService) : BaseApiController
{
    private readonly IAuthService _authService = authService;
    private readonly IUserService _userService = userService;

    /// <summary>
    /// Kullanıcı girişi yap ve JWT token al
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
        => ToActionResult(await _authService.LoginAsync(new LoginDto
        {
            Username = request.Username,
            Password = request.Password
        }, cancellationToken));

    /// <summary>
    /// İlk kurulum: Sistemde hiç kullanıcı yokken yönetici hesabı oluştur. Kullanıcı mevcut olduğunda bu uç çalışmaz.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var existing = await _userService.GetAllAsync(cancellationToken);
        if (existing.Success && existing.Data?.Any() == true)
            return StatusCode(403, new { message = "Sistem zaten yapılandırılmış. Yeni kullanıcı oluşturmak için giriş yapın." });

        return ToActionResult(await _userService.CreateAsync(new CreateUserDto
        {
            Username = request.Username,
            Password = request.Password
        }, cancellationToken));
    }
}