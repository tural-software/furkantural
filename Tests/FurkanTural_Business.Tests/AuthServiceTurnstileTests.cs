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

/// <summary>Giriş ucunda robot doğrulamasının kime sorulacağı. Karar, istemcinin gövdede bildirdiği uygulama adına bakarak verilemez: o alanı boş göndermek doğrulamadan kaçmanın en kısa yoluydu. Karar artık isteğin taşıdığı uygulama jetonuna bakar.<para>Kendini jetonla tanıtmayan çağıran için kural, giriş yapabilen ön-yüzlerin hepsi jeton alabilir hâle geldiğinde sertleşir. Bugün panelin jetonu olmadığı için sertleşmez; olsaydı yönetici kendi paneline giremezdi, çünkü panelin giriş ekranında doğrulama bileşeni yok.</para></summary>
public class AuthServiceTurnstileTests
{
    private const string Password = "dogru-parola";
    private const string Hashed = "hash:dogru-parola";

    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IRepository<Role>> _roles = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<ITurnstileVerifier> _turnstile = new();
    private readonly Mock<ILoginThrottle> _throttle = new();

    public AuthServiceTurnstileTests()
    {
        _hasher.Setup(h => h.IsHashed(It.IsAny<string?>())).Returns(true);
        _hasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string given, string stored) => given == Password && stored == Hashed);

        _turnstile.Setup(t => t.VerifyAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _throttle.Setup(t => t.GetRemainingLockout(It.IsAny<string?>())).Returns((TimeSpan?)null);

        _users.Setup(r => r.GetByUsernameForAdminAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User
            {
                Id = 7,
                Username = "deneme",
                Email = "deneme@ornek.test",
                Password = Hashed,
                RoleId = 2,
                IsActive = true,
                IsDeleted = false
            });

        _roles.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Role { Id = 2, Name = "User" });

        _uow.SetupGet(u => u.Users).Returns(_users.Object);
        _uow.SetupGet(u => u.Roles).Returns(_roles.Object);
    }

    private AuthService Build(params string[] registeredApps)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["JwtSettings:Secret"] = "birim-testleri-icin-yeterince-uzun-bir-imza-anahtari",
            ["JwtSettings:ExpiryMinutes"] = "60",
            ["Turnstile:RequiredApps:0"] = AppSourceDefinitions.Chat
        }).Build();

        var settings = new AppTokenSettings
        {
            Apps = registeredApps.Select(app => new AppRegistration { AppName = app, AppKey = "anahtar" }).ToList()
        };

        var logService = new Mock<ILogService>();
        logService.Setup(l => l.CreateAsync(It.IsAny<CreateLogDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<LogDto>.Ok(new LogDto()));

        return new AuthService(
            _uow.Object, _hasher.Object, configuration, Options.Create(settings),
            _turnstile.Object, _throttle.Object, Mock.Of<IAccountActivationService>(),
            new ActivityLogger(logService.Object, Mock.Of<IHttpContextAccessor>(), Mock.Of<IClock>()),
            Mock.Of<IClock>(c => c.UtcNow == new DateTime(2026, 9, 15, 12, 0, 0, DateTimeKind.Utc)));
    }

    private static Task<Result<LoginResultDto>> Login(AuthService sut, string? trustedAppSource, string? bodyAppSource = null)
        => sut.LoginAsync(
            new LoginDto { Username = "deneme", Password = Password, AppSource = bodyAppSource },
            "203.0.113.9", "Firefox", trustedAppSource);

    private void VerifyTurnstileAsked(Times times)
        => _turnstile.Verify(t => t.VerifyAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), times);

    [Fact]
    public async Task Listedeki_uygulamanin_jetonuyla_gelen_giriste_dogrulama_istenir()
    {
        var result = await Login(Build(AppSourceDefinitions.Chat), trustedAppSource: AppSourceDefinitions.Chat);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(400);
        VerifyTurnstileAsked(Times.Once());
    }

    [Fact]
    public async Task Listede_olmayan_uygulamanin_jetonuyla_dogrulama_istenmez()
    {
        var sut = Build(AppSourceDefinitions.Chat, AppSourceDefinitions.Admin);

        var result = await Login(sut, trustedAppSource: AppSourceDefinitions.Admin);

        result.Success.Should().BeTrue("panelin giriş ekranında doğrulama bileşeni yok; istenseydi yönetici kendi paneline giremezdi");
        VerifyTurnstileAsked(Times.Never());
    }

    [Fact]
    public async Task Panelin_jetonu_yokken_taninmayan_cagirandan_dogrulama_istenmez()
    {
        var sut = Build(AppSourceDefinitions.Chat);

        var result = await Login(sut, trustedAppSource: null);

        result.Success.Should().BeTrue();
        VerifyTurnstileAsked(Times.Never());
    }

    [Fact]
    public async Task Giris_yapabilen_tum_on_yuzler_jetonluyken_taninmayan_cagirandan_dogrulama_istenir()
    {
        var sut = Build(AppSourceDefinitions.Chat, AppSourceDefinitions.Admin);

        var result = await Login(sut, trustedAppSource: null);

        result.IsFailure.Should().BeTrue("ön-yüzlerin hepsi kendini tanıtabiliyorsa geriye kalan tanınmayan çağıran bir bot ya da betiktir");
        result.StatusCode.Should().Be(400);
        VerifyTurnstileAsked(Times.Once());
    }

    [Fact]
    public async Task Govdede_bildirilen_uygulama_adi_dogrulamadan_kacirmaz()
    {
        var sut = Build(AppSourceDefinitions.Chat, AppSourceDefinitions.Admin);

        var result = await Login(sut, trustedAppSource: AppSourceDefinitions.Chat, bodyAppSource: AppSourceDefinitions.Admin);

        result.IsFailure.Should().BeTrue("karar jetondan okunur; gövdedeki ad yalnızca bir etikettir");
        VerifyTurnstileAsked(Times.Once());
    }

    [Fact]
    public async Task Govdede_bildirilen_uygulama_adi_dogrulama_da_dayatmaz()
    {
        var sut = Build(AppSourceDefinitions.Chat);

        var result = await Login(sut, trustedAppSource: null, bodyAppSource: AppSourceDefinitions.Chat);

        result.Success.Should().BeTrue("gövde tek başına ne kaçırır ne dayatır; kararı yalnızca jeton verir");
        VerifyTurnstileAsked(Times.Never());
    }
}
