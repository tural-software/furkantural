using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using FurkanTural_Application.DTOs.Auth;
using FurkanTural_Application.DTOs.Log;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Settings;
using FurkanTural_Application.Wrappers;
using FurkanTural_Business.Helpers;
using FurkanTural_Business.Services.Concrete;
using FurkanTural_Domain.Constants;
using FurkanTural_Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Moq;

namespace FurkanTural_Business.Tests;

/// <summary>Kullanıcı jetonunun dokunulmazlık yaratmaması: her jeton güvenlik damgası ve ilk giriş anı taşır, yenileme damgayı veri tabanına karşı sınar ve oturumu mutlak ömrünün ötesine taşımaz.</summary>
public class AuthServiceSessionTests
{
    private const string Password = "dogru-parola";
    private const string Hashed = "hash:dogru-parola";
    private const string Stamp = "damga-1";

    private static readonly DateTime Now = new(2026, 9, 16, 12, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IRepository<Role>> _roles = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly User _user = new()
    {
        Id = 7, Username = "deneme", Email = "deneme@ornek.test", Password = Hashed, RoleId = 2,
        IsActive = true, IsDeleted = false, SecurityStamp = Stamp
    };

    private DateTime _now = Now;

    public AuthServiceSessionTests()
    {
        _hasher.Setup(h => h.IsHashed(It.IsAny<string?>())).Returns(true);
        _hasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string given, string stored) => given == Password && stored == Hashed);

        _users.Setup(r => r.GetByUsernameForAdminAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(_user);
        _users.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(_user);
        _roles.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(new Role { Id = 2, Name = "User" });

        _uow.SetupGet(u => u.Users).Returns(_users.Object);
        _uow.SetupGet(u => u.Roles).Returns(_roles.Object);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private AuthService Build(params string[] extraApps)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["JwtSettings:Secret"] = "birim-testleri-icin-yeterince-uzun-bir-imza-anahtari",
            ["JwtSettings:ExpiryMinutes"] = "60"
        }).Build();

        var settings = new AppTokenSettings
        {
            Apps = [new AppRegistration { AppName = AppSourceDefinitions.Chat, AppKey = "anahtar" },
                .. extraApps.Select(app => new AppRegistration { AppName = app, AppKey = "anahtar-" + app })]
        };

        var logService = new Mock<ILogService>();
        logService.Setup(l => l.CreateAsync(It.IsAny<CreateLogDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<LogDto>.Ok(new LogDto()));

        var throttle = new Mock<ILoginThrottle>();
        throttle.Setup(t => t.GetRemainingLockout(It.IsAny<string?>())).Returns((TimeSpan?)null);

        var turnstile = new Mock<ITurnstileVerifier>();
        turnstile.Setup(t => t.VerifyAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var clock = new Mock<IClock>();
        clock.SetupGet(c => c.UtcNow).Returns(() => _now);

        return new AuthService(
            _uow.Object, _hasher.Object, configuration, Options.Create(settings),
            turnstile.Object, throttle.Object, Mock.Of<IAccountActivationService>(),
            new ActivityLogger(logService.Object, Mock.Of<IHttpContextAccessor>(), clock.Object),
            clock.Object);
    }

    private static JwtSecurityToken Read(Result<LoginResultDto> result)
        => new JwtSecurityTokenHandler().ReadJwtToken(result.Data!.Token);

    private static string? ClaimOf(JwtSecurityToken token, string type)
        => token.Claims.FirstOrDefault(c => c.Type == type)?.Value;

    private static string? RoleOf(JwtSecurityToken token)
        => ClaimOf(token, "role") ?? ClaimOf(token, System.Security.Claims.ClaimTypes.Role);

    private void UserIsAdmin()
        => _roles.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(new Role { Id = 1, Name = "Admin" });

    [Fact]
    public async Task Chat_uzerinden_giren_yonetici_jetonda_uye_olarak_gorunur()
    {
        UserIsAdmin();

        var result = await Build().LoginAsync(
            new LoginDto { Username = "deneme", Password = Password, AppSource = AppSourceDefinitions.Chat }, null, null, AppSourceDefinitions.Chat);

        RoleOf(Read(result)).Should().Be("User",
            "Chat oturumu yedi gün yaşar ve BFF API'nin her yoluna vekillik eder; yönetici rolü taşısaydı Chat çerezi " +
            "panelin bir saatlik kuralını aşan bir yönetici kimliği olurdu");
        result.Data!.RoleName.Should().Be("User");
        ClaimOf(Read(result), ClaimDefinitions.AppSource).Should().Be(AppSourceDefinitions.Chat);
    }

    [Fact]
    public async Task Panelden_giren_yonetici_yonetici_kalir()
    {
        UserIsAdmin();

        var result = await Build().LoginAsync(new LoginDto { Username = "deneme", Password = Password }, null, null, AppSourceDefinitions.Admin);

        RoleOf(Read(result)).Should().Be("Admin");
        result.Data!.RoleName.Should().Be("Admin", "panel girişi rolü yanıttan okuyup yönetici olmayanı geri çevirir");
    }

    [Fact]
    public async Task Kayitli_panel_kaynagiyla_giren_yonetici_yonetici_kalir()
    {
        UserIsAdmin();

        var result = await Build(AppSourceDefinitions.Admin).LoginAsync(
            new LoginDto { Username = "deneme", Password = Password, AppSource = AppSourceDefinitions.Admin }, null, null, AppSourceDefinitions.Admin);

        RoleOf(Read(result)).Should().Be("Admin");
    }

    [Fact]
    public async Task Chat_oturumundaki_yonetici_jetonu_yenilemede_uyeye_iner()
    {
        UserIsAdmin();

        var result = await Build().RefreshAsync(7, AppSourceDefinitions.Chat, Stamp, Now.AddDays(-2));

        result.Success.Should().BeTrue();
        RoleOf(Read(result)).Should().Be("User",
            "yayından önce verilmiş yedi günlük Chat oturumları ilk yenilemede yönetici rolünü bırakmalı");
    }

    [Fact]
    public async Task Chat_uzerinden_giren_uyenin_rolu_degismez()
    {
        var result = await Build().LoginAsync(
            new LoginDto { Username = "deneme", Password = Password, AppSource = AppSourceDefinitions.Chat }, null, null, AppSourceDefinitions.Chat);

        RoleOf(Read(result)).Should().Be("User");
    }

    [Fact]
    public async Task Giris_jetonu_damgayi_ve_giris_anini_tasir()
    {
        var result = await Build().LoginAsync(new LoginDto { Username = "deneme", Password = Password }, null, null, AppSourceDefinitions.Admin);

        var token = Read(result);
        ClaimOf(token, ClaimDefinitions.SecurityStamp).Should().Be(Stamp);
        ClaimOf(token, ClaimDefinitions.AuthTime).Should().Be(new DateTimeOffset(Now).ToUnixTimeSeconds().ToString());
    }

    [Fact]
    public async Task Damgasi_olmayan_kullaniciya_giriste_damga_uretilir()
    {
        _user.SecurityStamp = null;

        var result = await Build().LoginAsync(new LoginDto { Username = "deneme", Password = Password }, null, null, AppSourceDefinitions.Admin);

        _user.SecurityStamp.Should().NotBeNullOrEmpty("damgasız jeton hiçbir istekte yeniden doğrulanamazdı");
        ClaimOf(Read(result), ClaimDefinitions.SecurityStamp).Should().Be(_user.SecurityStamp);
        _users.Verify(r => r.UpdateAsync(_user, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Panel_oturumunun_jetonu_bir_saati_asamaz()
    {
        var result = await Build().LoginAsync(new LoginDto { Username = "deneme", Password = Password }, null, null, AppSourceDefinitions.Admin);

        result.Data!.ExpiresAt.Should().BeOnOrBefore(Now.AddHours(1));
    }

    [Fact]
    public async Task Yenileme_giris_anini_tasir_ve_sifirlamaz()
    {
        var authTime = Now.AddMinutes(-40);

        var result = await Build().RefreshAsync(7, null, Stamp, authTime);

        result.Success.Should().BeTrue();
        ClaimOf(Read(result), ClaimDefinitions.AuthTime).Should().Be(new DateTimeOffset(authTime).ToUnixTimeSeconds().ToString(),
            "yenileme giriş anını sıfırlasaydı etkin kalan bir oturum süresiz uzardı");
        result.Data!.ExpiresAt.Should().Be(authTime.AddHours(1), "jeton oturumun mutlak bitişini aşmamalı");
    }

    [Theory]
    [InlineData(null, 60)]
    [InlineData(AppSourceDefinitions.Chat, 7 * 24 * 60)]
    public async Task Mutlak_omrunu_dolduran_oturum_yenilenmez(string? appSource, int lifetimeMinutes)
    {
        var result = await Build().RefreshAsync(7, appSource, Stamp, Now.AddMinutes(-lifetimeMinutes));

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task Chat_oturumu_yedi_gun_icinde_yenilenir()
    {
        var result = await Build().RefreshAsync(7, AppSourceDefinitions.Chat, Stamp, Now.AddDays(-6));

        result.Success.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("eski-damga")]
    public async Task Damgasi_eslesmeyen_jeton_yenilenmez(string? stamp)
    {
        var result = await Build().RefreshAsync(7, null, stamp, Now.AddMinutes(-5));

        result.IsFailure.Should().BeTrue(
            "yenileme damgaya bakmasaydı parolası değiştirilmiş hesabın çalınmış eski jetonu güncel damgalı bir jetona çevrilebilirdi");
        result.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task Giris_ani_olmayan_jeton_yenilenmez()
    {
        var result = await Build().RefreshAsync(7, null, Stamp, null);

        result.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task Kayit_jetonu_chat_oturumu_olarak_verilir()
    {
        _users.Setup(r => r.GetByUsernameForAdminAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        _users.Setup(r => r.GetByEmailForAdminAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        _roles.Setup(r => r.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Role, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Role { Id = 2, Name = "User" });
        _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns(Hashed);

        var result = await Build().RegisterAsync(new FurkanTural_Application.DTOs.User.RegisterDto
        {
            Username = "yenikullanici", Email = "yeni@ornek.test", Password = "Yeni-Parola7",
            AcceptAgreement = true, ConfirmAdult = true
        }, null, null);

        var token = Read(result);
        ClaimOf(token, ClaimDefinitions.AppSource).Should().Be(AppSourceDefinitions.Chat,
            "kendi kendine kayıt yalnızca Chatural üyeliğidir; kaynak düşerse yeni üye bir saat sonra oturumdan atılırdı");
        ClaimOf(token, ClaimDefinitions.SecurityStamp).Should().NotBeNullOrEmpty();
    }
}
