using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FurkanTural_Application.DTOs.Auth;
using FurkanTural_Application.DTOs.User;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Settings;
using FurkanTural_Application.Wrappers;
using FurkanTural_Business.Helpers;
using FurkanTural_Domain.Constants;
using FurkanTural_Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FurkanTural_Business.Services.Concrete;

/// <summary>Var olmayan kullanıcı adında da parola doğrulaması çalıştırılır: sabit bir kukla özet üzerinde gerçek bir PBKDF2 hesabı yapılır. Amaç yanıt süresini eşitlemektir — hemen dönülseydi süre farkı hangi kullanıcı adlarının kayıtlı olduğunu sayılabilir hâle getirirdi. Kukla özet süreç başına bir kez üretilir, çünkü her istekte üretmek savunmanın kendisini yük hâline getirirdi.<para>Başarılı giriş kayda yazabilir: parola eski geri çözülebilir biçimde saklanıyorsa doğrulandığı anda özet biçimine taşınır (bkz. <see cref="IPasswordHasher"/>). Böylece havuz ayrı bir taşıma işi olmadan zamanla dönüşür, ama okuma gibi görünen bir uç yazma yapar.</para><para>Turnstile zorunluluğu <c>Turnstile:RequiredApps</c> listesine bakar ve yalnızca LoginAsync için geçerlidir; AppSource boş gelirse doğrulama hiç istenmez. RegisterAsync ise listeye bakmadan her çağrıda doğrulama uygular.</para><para>Her iki uç da kullanıcıyı global süzgeci atlayarak okur (bkz. <see cref="IUserRepository"/>); silinmiş ve pasif satırları da görürler, çünkü pasif hesabın açılması ancak onu görebilmekle mümkün ve tekil indeksler o kullanıcı adlarını hâlâ tutuyor.</para><para>LoginAsync'te silinmiş hesap, var olmayan kullanıcı adıyla aynı dala düşer: aynı metin, aynı 401 ve aynı kukla özet hesabı. Pasif hesap ise yalnızca parola doğrulandıktan sonra ayrışır ve doğrulama postasını orada tetikler. Sıralama savunmanın kendisidir — parolayı bilmeden tetiklenebilseydi uç, herhangi birinin istediği adrese posta yollatabildiği bir mekanizmaya dönerdi. Doğru parolayı verene hesabın kapalı olduğunu söylemek bir şey ele vermez; zaten kimlik bilgisi elinde olan biri bunu başka yollarla da öğrenir, söylememek ise onu yalnızca çıkışsız bırakırdı.</para><para>RegisterAsync'te üç durumun üçü de dışarıya aynı metni döndürür, hangisinin tetiklendiği yalnızca istemciye çıkmayan InternalMessage'da durur — bu ayrımı yanıta taşımak hesabın silinmiş mi pasif mi olduğunu ele verirdi. Pasif dal yeni satır açmaz, doğrulama postası gönderir; kullanıcı kendi hesabını yeniden kayıt olarak geri istiyorsa alacağı şey eski hesabıdır. Girilen parola bilerek yok sayılır, aksi hâlde adresin sahibi olmayan biri parola değiştirmeyi tetikleyebilirdi.</para></summary>
public class AuthService(
    IUnitOfWork unitOfWork,
    IEncryptionService encryptionService,
    IPasswordHasher passwordHasher,
    IConfiguration configuration,
    IOptions<AppTokenSettings> appTokenSettings,
    ITurnstileVerifier turnstileVerifier,
    ILoginThrottle loginThrottle,
    IAccountActivationService accountActivationService,
    ActivityLogger activityLogger,
    IClock clock) : IAuthService
{
    private readonly IAccountActivationService _accountActivationService = accountActivationService;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IEncryptionService _encryptionService = encryptionService;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;
    private readonly IConfiguration _configuration = configuration;
    private readonly AppTokenSettings _appTokenSettings = appTokenSettings.Value;
    private readonly ITurnstileVerifier _turnstileVerifier = turnstileVerifier;
    private readonly ILoginThrottle _loginThrottle = loginThrottle;
    private readonly ActivityLogger _activityLogger = activityLogger;
    private readonly IClock _clock = clock;

    private static readonly Lazy<string> DummyHash = new(() =>
        new PasswordHasher().Hash("login-timing-defense-placeholder"),
        LazyThreadSafetyMode.ExecutionAndPublication);

    public async Task<Result<LoginResultDto>> LoginAsync(LoginDto dto, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default)
    {
        if (IsTurnstileRequired(dto.AppSource) &&
            !await _turnstileVerifier.VerifyAsync(dto.TurnstileToken, null, cancellationToken))
        {
            await _activityLogger.LogWarningAsync(
                $"Giriş reddedildi: robot doğrulaması başarısız. Kullanıcı: {Ad(dto.Username)}, uygulama: {Ad(dto.AppSource)}", cancellationToken);
            return Result<LoginResultDto>.Fail("Robot doğrulaması başarısız oldu. Lütfen tekrar deneyin.", statusCode: 400);
        }

        if (string.IsNullOrWhiteSpace(dto.Username))
            return Result<LoginResultDto>.Fail("Kullanıcı adı boş olamaz.");

        if (string.IsNullOrWhiteSpace(dto.Password))
            return Result<LoginResultDto>.Fail("Şifre boş olamaz.");

        if (_loginThrottle.GetRemainingLockout(dto.Username) is { } remaining)
        {
            await _activityLogger.LogWarningAsync(
                $"Giriş reddedildi: çok fazla hatalı deneme. Kullanıcı: {Ad(dto.Username)}", cancellationToken);
            return Result<LoginResultDto>.Fail(
                $"Çok fazla hatalı deneme yapıldı. Lütfen {Math.Ceiling(remaining.TotalSeconds)} saniye sonra tekrar deneyin.",
                statusCode: 429);
        }

        var user = await _unitOfWork.Users.GetByUsernameForAdminAsync(dto.Username, cancellationToken);
        if (user is null || user.IsDeleted)
        {
            _passwordHasher.Verify(dto.Password, DummyHash.Value);
            _loginThrottle.RegisterFailure(dto.Username);
            await _activityLogger.LogWarningAsync(
                $"Giriş reddedildi: kullanıcı bulunamadı veya silinmiş. Kullanıcı: {Ad(dto.Username)}", cancellationToken);
            return Result<LoginResultDto>.Fail("Kullanıcı adı veya şifre hatalı.",
                user is null ? string.Empty : $"Giriş reddedildi: #{user.Id} silinmiş hesap.", 401);
        }

        if (string.IsNullOrWhiteSpace(user.Password))
        {
            _loginThrottle.RegisterFailure(dto.Username);
            await _activityLogger.LogWarningAsync(
                $"Giriş reddedildi: #{user.Id} hesabında parola kayıtlı değil.", cancellationToken);
            return Result<LoginResultDto>.Fail("Kullanıcı adı veya şifre hatalı.", statusCode: 401);
        }

        bool passwordValid;
        if (_passwordHasher.IsHashed(user.Password))
        {
            passwordValid = _passwordHasher.Verify(dto.Password, user.Password);
        }
        else
        {
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
            await _activityLogger.LogWarningAsync(
                $"Giriş reddedildi: #{user.Id} parola hatalı.", cancellationToken);
            return Result<LoginResultDto>.Fail("Kullanıcı adı veya şifre hatalı.", statusCode: 401);
        }

        _loginThrottle.Reset(dto.Username);

        if (!user.IsActive)
        {
            var issued = await _accountActivationService.IssueAsync(user.Id, "Login", ipAddress, userAgent, cancellationToken);

            await _activityLogger.LogWarningAsync(
                $"Giriş reddedildi: #{user.Id} hesap pasif.", cancellationToken);

            return Result<LoginResultDto>.Fail(
                "Hesabınız kapalı. Kayıtlı e-posta adresinize hesabı yeniden açma bağlantısı gönderdik. Adresinize ulaşamıyorsanız iletişim formundan bize yazın.",
                issued.IsFailure
                    ? $"Giriş reddedildi: #{user.Id} pasif hesap; aktivasyon gönderilemedi: {issued.InternalMessage}"
                    : $"Giriş reddedildi: #{user.Id} pasif hesap; aktivasyon tetiklendi.",
                403);
        }

        var role = await _unitOfWork.Roles.GetByIdAsync(user.RoleId, cancellationToken);
        var roleName = role?.Name ?? "User";

        await _activityLogger.LogAsync(
            $"Giriş yapıldı: #{user.Id} ({roleName}), uygulama: {Ad(dto.AppSource)}", cancellationToken);

        return Result<LoginResultDto>.Ok(BuildLoginResult(user, roleName, dto.AppSource));
    }

    public async Task<Result<LoginResultDto>> RegisterAsync(RegisterDto dto, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default)
    {
        if (!await _turnstileVerifier.VerifyAsync(dto.TurnstileToken, null, cancellationToken))
        {
            await _activityLogger.LogWarningAsync(
                $"Kayıt reddedildi: robot doğrulaması başarısız. Kullanıcı: {Ad(dto.Username)}", cancellationToken);
            return Result<LoginResultDto>.Fail("Robot doğrulaması başarısız oldu. Lütfen tekrar deneyin.", statusCode: 400);
        }

        if (string.IsNullOrWhiteSpace(dto.Username))
            return Result<LoginResultDto>.Fail("Kullanıcı adı boş olamaz.");

        if (string.IsNullOrWhiteSpace(dto.Email))
            return Result<LoginResultDto>.Fail("E-posta boş olamaz.");

        if (string.IsNullOrWhiteSpace(dto.Password))
            return Result<LoginResultDto>.Fail("Şifre boş olamaz.");

        if (!dto.AcceptAgreement)
            return Result<LoginResultDto>.Fail("Üyelik sözleşmesini onaylamadan kayıt olamazsınız.");

        var usernameOwner = await _unitOfWork.Users.GetByUsernameForAdminAsync(dto.Username, cancellationToken);
        if (usernameOwner is not null)
            return await RegistrationRefusedAsync(usernameOwner, "Bu kullanıcı adı zaten kullanılıyor.", ipAddress, userAgent, cancellationToken);

        var emailOwner = await _unitOfWork.Users.GetByEmailForAdminAsync(dto.Email, cancellationToken);
        if (emailOwner is not null)
            return await RegistrationRefusedAsync(emailOwner, "Bu e-posta adresi zaten kullanılıyor.", ipAddress, userAgent, cancellationToken);

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
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return Result<LoginResultDto>.Fail("Kullanıcı bulunamadı.", statusCode: 401);

        var role = await _unitOfWork.Roles.GetByIdAsync(user.RoleId, cancellationToken);
        var roleName = role?.Name ?? "User";

        return Result<LoginResultDto>.Ok(BuildLoginResult(user, roleName, appSource));
    }

    private const string ReopenHint = " Hesap sizinse ve kapalıysa, kayıtlı adresinize hesabı yeniden açma bağlantısı gönderilir.";

    private async Task<Result<LoginResultDto>> RegistrationRefusedAsync(
        User owner, string message, string? ipAddress, string? userAgent, CancellationToken cancellationToken)
    {
        if (owner.IsDeleted)
            return Result<LoginResultDto>.Fail(message + ReopenHint, $"Kayıt reddedildi: #{owner.Id} silinmiş hesap.");

        if (owner.IsActive)
            return Result<LoginResultDto>.Fail(message + ReopenHint);

        var issued = await _accountActivationService.IssueAsync(owner.Id, "Register", ipAddress, userAgent, cancellationToken);

        return Result<LoginResultDto>.Fail(message + ReopenHint,
            issued.IsFailure
                ? $"Kayıt reddedildi: #{owner.Id} pasif hesap; aktivasyon gönderilemedi: {issued.InternalMessage}"
                : $"Kayıt reddedildi: #{owner.Id} pasif hesap; aktivasyon tetiklendi.");
    }

    private static string Ad(string? deger) => string.IsNullOrWhiteSpace(deger) ? "(boş)" : deger.Trim();

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
