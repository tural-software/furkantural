using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FurkanTural_Application.DTOs.Auth;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Settings;
using FurkanTural_Application.Wrappers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FurkanTural_Business.Services.Concrete;

public class AuthService(
    IUnitOfWork unitOfWork,
    IEncryptionService encryptionService,
    IConfiguration configuration,
    IOptions<AppTokenSettings> appTokenSettings) : IAuthService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IEncryptionService _encryptionService = encryptionService;
    private readonly IConfiguration _configuration = configuration;
    private readonly AppTokenSettings _appTokenSettings = appTokenSettings.Value;

    public async Task<Result<LoginResultDto>> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Username))
            return Result<LoginResultDto>.Fail("Kullanıcı adı boş olamaz.");

        if (string.IsNullOrWhiteSpace(dto.Password))
            return Result<LoginResultDto>.Fail("Şifre boş olamaz.");

        var user = await _unitOfWork.Users.GetAsync(x => x.Username == dto.Username, cancellationToken);
        if (user is null)
            return Result<LoginResultDto>.Fail("Kullanıcı adı veya şifre hatalı.", statusCode: 401);

        if (string.IsNullOrWhiteSpace(user.Password))
            return Result<LoginResultDto>.Fail("Kullanıcı adı veya şifre hatalı.", statusCode: 401);

        var decryptResult = _encryptionService.Decrypt(user.Password);
        if (decryptResult.IsFailure)
            return Result<LoginResultDto>.Fail("Kullanıcı adı veya şifre hatalı.", statusCode: 401);

        if (decryptResult.Data != dto.Password)
            return Result<LoginResultDto>.Fail("Kullanıcı adı veya şifre hatalı.", statusCode: 401);

        var role = await _unitOfWork.Roles.GetByIdAsync(user.RoleId, cancellationToken);
        var roleName = role?.Name ?? "User";

        var (secret, issuer, audience, expiryMinutes) = GetJwtConfig();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.Username ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Role, roleName)
        };

        if (!string.IsNullOrWhiteSpace(dto.AppSource))
            claims.Add(new Claim("app_source", dto.AppSource));

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return Result<LoginResultDto>.Ok(new LoginResultDto
        {
            Token = tokenString,
            Username = user.Username,
            RoleName = roleName,
            ExpiresAt = expiresAt
        });
    }

    public Task<Result<LoginResultDto>> GenerateAppTokenAsync(AppTokenRequestDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.AppKey))
            return Task.FromResult(Result<LoginResultDto>.Fail("AppKey boş olamaz.", statusCode: 400));

        if (string.IsNullOrWhiteSpace(dto.AppName))
            return Task.FromResult(Result<LoginResultDto>.Fail("AppName boş olamaz.", statusCode: 400));

        var registered = _appTokenSettings.Apps
            .FirstOrDefault(a => a.AppKey == dto.AppKey && a.AppName == dto.AppName);

        if (registered is null)
            return Task.FromResult(Result<LoginResultDto>.Fail("Geçersiz uygulama kimlik bilgileri.", statusCode: 401));

        var (secret, issuer, audience, _) = GetJwtConfig();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddDays(_appTokenSettings.ExpiryDays);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "Visitor"),
            new Claim("app_source", registered.AppName)
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return Task.FromResult(Result<LoginResultDto>.Ok(new LoginResultDto
        {
            Token = tokenString,
            Username = registered.AppName,
            RoleName = "Visitor",
            ExpiresAt = expiresAt
        }));
    }

    private (string secret, string issuer, string audience, int expiryMinutes) GetJwtConfig()
    {
        var secret = _configuration["JwtSettings:Secret"]
            ?? throw new InvalidOperationException("JwtSettings:Secret yapılandırılmamış.");
        var issuer = _configuration["JwtSettings:Issuer"] ?? "FurkanTural";
        var audience = _configuration["JwtSettings:Audience"] ?? "FurkanTuralClient";
        var expiryMinutes = int.TryParse(_configuration["JwtSettings:ExpiryMinutes"], out var mins) ? mins : 60;
        return (secret, issuer, audience, expiryMinutes);
    }
}