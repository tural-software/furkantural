using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FurkanTural_Application.DTOs.Auth;
using FurkanTural_Application.DTOs.User;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Settings;
using FurkanTural_Application.Wrappers;
using FurkanTural_Domain.Constants;
using FurkanTural_Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FurkanTural_Business.Services.Concrete;

public class AuthService(
    IUnitOfWork unitOfWork,
    IEncryptionService encryptionService,
    IPasswordHasher passwordHasher,
    IConfiguration configuration,
    IOptions<AppTokenSettings> appTokenSettings,
    ITurnstileVerifier turnstileVerifier,
    ILoginThrottle loginThrottle,
    IClock clock) : IAuthService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IEncryptionService _encryptionService = encryptionService;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;
    private readonly IConfiguration _configuration = configuration;
    private readonly AppTokenSettings _appTokenSettings = appTokenSettings.Value;
    private readonly ITurnstileVerifier _turnstileVerifier = turnstileVerifier;
    private readonly ILoginThrottle _loginThrottle = loginThrottle;
    private readonly IClock _clock = clock;

    // Zamanlama savunması için sabit bir kukla hash. Lazy: PBKDF2 üretimi pahalıdır, süreç
    // başına YALNIZCA BİR KEZ hesaplanır (her istekte üretilseydi savunma kendisi bir yük olurdu).
    // Değeri hiçbir hesapla eşleşmez; yalnızca Verify'ın CPU maliyetini ödetmek için vardır.
    private static readonly Lazy<string> DummyHash = new(() =>
        new PasswordHasher().Hash("login-timing-defense-placeholder"),
        LazyThreadSafetyMode.ExecutionAndPublication);

    public async Task<Result<LoginResultDto>> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default)
    {
        // Turnstile yalnızca onu kullanan uygulamalar için (örn. Chat). Admin gibi
        // token göndermeyen istemciler etkilenmez. (Register ucu Chat'e özeldir; orada koşulsuz.)
        if (IsTurnstileRequired(dto.AppSource) &&
            !await _turnstileVerifier.VerifyAsync(dto.TurnstileToken, null, cancellationToken))
            return Result<LoginResultDto>.Fail("Robot doğrulaması başarısız oldu. Lütfen tekrar deneyin.", statusCode: 400);

        if (string.IsNullOrWhiteSpace(dto.Username))
            return Result<LoginResultDto>.Fail("Kullanıcı adı boş olamaz.");

        if (string.IsNullOrWhiteSpace(dto.Password))
            return Result<LoginResultDto>.Fail("Şifre boş olamaz.");

        // Brute-force savunması: parola DOĞRULANMADAN önce kilit kontrolü yapılır ki kilitliyken
        // hiç hash hesaplanmasın (PBKDF2 pahalıdır → aksi halde ucuz bir CPU tüketim vektörü olurdu).
        if (_loginThrottle.GetRemainingLockout(dto.Username) is { } remaining)
            return Result<LoginResultDto>.Fail(
                $"Çok fazla hatalı deneme yapıldı. Lütfen {Math.Ceiling(remaining.TotalSeconds)} saniye sonra tekrar deneyin.",
                statusCode: 429);

        var user = await _unitOfWork.Users.GetAsync(x => x.Username == dto.Username, cancellationToken);
        if (user is null)
        {
            // ZAMANLAMA YAN KANALI SAVUNMASI: Var olmayan kullanıcıda hemen dönseydik yanıt
            // ~5 ms, var olan kullanıcıda PBKDF2 doğrulaması yüzünden ~150 ms sürerdi; saldırgan
            // bu farkı ölçüp hangi kullanıcı adlarının var olduğunu sayabilirdi. Sahte bir hash'e
            // karşı gerçek bir doğrulama çalıştırarak aynı maliyeti bilinçli olarak ödüyoruz.
            _passwordHasher.Verify(dto.Password, DummyHash.Value);

            // Var olmayan kullanıcı için de sayaç işletilir — "kilitlendi" yanıtının hesabın
            // varlığını ele vermemesi için.
            _loginThrottle.RegisterFailure(dto.Username);
            return Result<LoginResultDto>.Fail("Kullanıcı adı veya şifre hatalı.", statusCode: 401);
        }

        if (string.IsNullOrWhiteSpace(user.Password))
        {
            _loginThrottle.RegisterFailure(dto.Username);
            return Result<LoginResultDto>.Fail("Kullanıcı adı veya şifre hatalı.", statusCode: 401);
        }

        bool passwordValid;
        if (_passwordHasher.IsHashed(user.Password))
        {
            passwordValid = _passwordHasher.Verify(dto.Password, user.Password);
        }
        else
        {
            // Legacy kayıt: geri çözülebilir AES. Doğruysa şeffaf olarak PBKDF2'ye taşı —
            // böylece parola havuzu zamanla tek yönlü hash'e döner, ek migration gerekmez.
            var decryptResult = _encryptionService.Decrypt(user.Password);
            passwordValid = !decryptResult.IsFailure && decryptResult.Data == dto.Password;

            if (passwordValid)
            {
                user.Password = _passwordHasher.Hash(dto.Password);
                await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }

        if (!passwordValid)
        {
            _loginThrottle.RegisterFailure(dto.Username);
            return Result<LoginResultDto>.Fail("Kullanıcı adı veya şifre hatalı.", statusCode: 401);
        }

        // Başarılı giriş sayacı sıfırlar: meşru kullanıcı birkaç kez yanılıp sonra doğru
        // girdiğinde birikmiş denemeler onu sonradan kilitlemesin.
        _loginThrottle.Reset(dto.Username);

        var role = await _unitOfWork.Roles.GetByIdAsync(user.RoleId, cancellationToken);
        var roleName = role?.Name ?? "User";

        return Result<LoginResultDto>.Ok(BuildLoginResult(user, roleName, dto.AppSource));
    }

    public async Task<Result<LoginResultDto>> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default)
    {
        if (!await _turnstileVerifier.VerifyAsync(dto.TurnstileToken, null, cancellationToken))
            return Result<LoginResultDto>.Fail("Robot doğrulaması başarısız oldu. Lütfen tekrar deneyin.", statusCode: 400);

        if (string.IsNullOrWhiteSpace(dto.Username))
            return Result<LoginResultDto>.Fail("Kullanıcı adı boş olamaz.");

        if (string.IsNullOrWhiteSpace(dto.Email))
            return Result<LoginResultDto>.Fail("E-posta boş olamaz.");

        if (string.IsNullOrWhiteSpace(dto.Password))
            return Result<LoginResultDto>.Fail("Şifre boş olamaz.");

        if (!dto.AcceptAgreement)
            return Result<LoginResultDto>.Fail("Üyelik sözleşmesini onaylamadan kayıt olamazsınız.");

        var usernameExists = await _unitOfWork.Users.AnyAsync(x => x.Username == dto.Username, cancellationToken);
        if (usernameExists)
            return Result<LoginResultDto>.Fail("Bu kullanıcı adı zaten kullanılıyor.");

        var emailExists = await _unitOfWork.Users.AnyAsync(x => x.Email == dto.Email, cancellationToken);
        if (emailExists)
            return Result<LoginResultDto>.Fail("Bu e-posta adresi zaten kullanılıyor.");

        var role = await _unitOfWork.Roles.GetAsync(x => x.Name == "User", cancellationToken);
        if (role is null)
            return Result<LoginResultDto>.Fail("Üyelik rolü yapılandırılmamış.", statusCode: 500);

        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            DisplayName = string.IsNullOrWhiteSpace(dto.DisplayName) ? dto.Username : dto.DisplayName,
            Password = _passwordHasher.Hash(dto.Password),
            RoleId = role.Id,
            MembershipAgreementAcceptedAt = _clock.UtcNow,
            MembershipAgreementVersion = AgreementDefinitions.CurrentVersion
        };

        await _unitOfWork.Users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<LoginResultDto>.Ok(BuildLoginResult(user, role.Name ?? "User"));
    }

    public async Task<Result<LoginResultDto>> RefreshAsync(int userId, string? appSource, CancellationToken cancellationToken = default)
    {
        // Kullanıcı hâlâ mevcut/aktif mi? (Silinen/pasifleştirilen hesap token yenileyemez.)
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return Result<LoginResultDto>.Fail("Kullanıcı bulunamadı.", statusCode: 401);

        var role = await _unitOfWork.Roles.GetByIdAsync(user.RoleId, cancellationToken);
        var roleName = role?.Name ?? "User";

        return Result<LoginResultDto>.Ok(BuildLoginResult(user, roleName, appSource));
    }

    // Turnstile, config'deki Turnstile:RequiredApps listesindeki AppSource'lar için zorunludur.
    private bool IsTurnstileRequired(string? appSource)
    {
        if (string.IsNullOrWhiteSpace(appSource)) return false;
        return _configuration.GetSection("Turnstile:RequiredApps")
            .GetChildren()
            .Any(c => string.Equals(c.Value, appSource, StringComparison.OrdinalIgnoreCase));
    }

    private LoginResultDto BuildLoginResult(User user, string roleName, string? appSource = null)
    {
        var (secret, issuer, audience, expiryMinutes) = GetJwtConfig();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = _clock.UtcNow.AddMinutes(expiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.Username ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Role, roleName)
        };

        if (!string.IsNullOrWhiteSpace(appSource))
            claims.Add(new Claim("app_source", appSource));

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return new LoginResultDto
        {
            Token = tokenString,
            UserId = user.Id,
            Username = user.Username,
            RoleName = roleName,
            AvatarUrl = user.AvatarUrl,
            ExpiresAt = expiresAt,
            // Geçerli sürümü kabul etmiş mi? (Sürüm artarsa eski onay "kabul edilmemiş" sayılır.)
            MembershipAgreementAccepted = user.MembershipAgreementAcceptedAt != null
                && user.MembershipAgreementVersion == AgreementDefinitions.CurrentVersion
        };
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
        var expiresAt = _clock.UtcNow.AddDays(_appTokenSettings.ExpiryDays);

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