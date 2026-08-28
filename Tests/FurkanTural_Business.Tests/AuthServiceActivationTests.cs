using FluentAssertions;
using FurkanTural_Application.DTOs.Auth;
using FurkanTural_Application.DTOs.User;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Settings;
using FurkanTural_Application.DTOs.Log;
using FurkanTural_Application.Wrappers;
using FurkanTural_Business.Helpers;
using FurkanTural_Business.Services.Concrete;
using Microsoft.AspNetCore.Http;
using FurkanTural_Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Moq;

namespace FurkanTural_Business.Tests;

/// <summary>Planın dört senaryosu: silinmiş hesabın bilgileriyle giriş ve kayıt reddedilir, pasif hesabın bilgileriyle giriş ve kayıt doğrulama postası tetikler.</summary>
public class AuthServiceActivationTests
{
    private const string Password = "dogru-parola";
    private const string Hashed = "hash:dogru-parola";

    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IRepository<Role>> _roles = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<ITurnstileVerifier> _turnstile = new();
    private readonly Mock<ILoginThrottle> _throttle = new();
    private readonly Mock<IAccountActivationService> _activation = new();
    private readonly Mock<ILogService> _logService = new();
    private readonly List<CreateLogDto> _logged = [];
    private readonly List<User> _created = [];

    private readonly AuthService _sut;

    public AuthServiceActivationTests()
    {
        _hasher.Setup(h => h.IsHashed(It.IsAny<string?>())).Returns(true);
        _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns(Hashed);
        _hasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string given, string stored) => given == Password && stored == Hashed);

        _turnstile.Setup(t => t.VerifyAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _logService.Setup(l => l.CreateAsync(It.IsAny<CreateLogDto>(), It.IsAny<CancellationToken>()))
            .Callback<CreateLogDto, CancellationToken>((dto, _) => _logged.Add(dto))
            .ReturnsAsync(Result<LogDto>.Ok(new LogDto()));

        _throttle.Setup(t => t.GetRemainingLockout(It.IsAny<string?>())).Returns((TimeSpan?)null);

        _activation.Setup(a => a.IssueAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        _roles.Setup(r => r.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Role, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Role { Id = 2, Name = "User" });
        _roles.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Role { Id = 2, Name = "User" });

        _users.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => _created.Add(u))
            .Returns(Task.CompletedTask);

        _uow.SetupGet(u => u.Users).Returns(_users.Object);
        _uow.SetupGet(u => u.Roles).Returns(_roles.Object);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["JwtSettings:Secret"] = "birim-testleri-icin-yeterince-uzun-bir-imza-anahtari",
            ["JwtSettings:ExpiryMinutes"] = "60"
        }).Build();

        _sut = new AuthService(
            _uow.Object,
            Mock.Of<IEncryptionService>(),
            _hasher.Object,
            configuration,
            Options.Create(new AppTokenSettings()),
            _turnstile.Object,
            _throttle.Object,
            _activation.Object,
            new ActivityLogger(_logService.Object, Mock.Of<IHttpContextAccessor>(), Mock.Of<IClock>()),
            Mock.Of<IClock>(c => c.UtcNow == new DateTime(2026, 8, 24, 12, 0, 0, DateTimeKind.Utc)));
    }

    private static User Account(bool isActive, bool isDeleted, int id = 7) => new()
    {
        Id = id,
        Username = "deneme",
        Email = "deneme@ornek.test",
        Password = Hashed,
        RoleId = 2,
        IsActive = isActive,
        IsDeleted = isDeleted
    };

    private void ByUsername(User? user)
        => _users.Setup(r => r.GetByUsernameForAdminAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);

    private void ByEmail(User? user)
        => _users.Setup(r => r.GetByEmailForAdminAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);

    private Task<Result<LoginResultDto>> Login(string password = Password)
        => _sut.LoginAsync(new LoginDto { Username = "deneme", Password = password, AppSource = "Chat" },
            "203.0.113.9", "Firefox");

    private Task<Result<LoginResultDto>> Register()
        => _sut.RegisterAsync(new RegisterDto
        {
            Username = "deneme",
            Email = "deneme@ornek.test",
            Password = "Yeni-Parola7",
            AcceptAgreement = true
        }, "203.0.113.9", "Firefox");

    private void VerifyNoActivation()
        => _activation.Verify(a => a.IssueAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);

    [Fact]
    public async Task Silinmis_hesabin_bilgileriyle_giris_reddedilir()
    {
        ByUsername(Account(isActive: false, isDeleted: true));

        var result = await Login();

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(401);
        result.Errors[0].Should().Be("Kullanıcı adı veya şifre hatalı.");
        VerifyNoActivation();
    }

    [Fact]
    public async Task Silinmis_hesap_var_olmayan_kullanici_adiyla_ayni_yaniti_alir()
    {
        ByUsername(null);
        var bilinmeyen = await Login();

        ByUsername(Account(isActive: false, isDeleted: true));
        var silinmis = await Login();

        silinmis.StatusCode.Should().Be(bilinmeyen.StatusCode);
        silinmis.Errors.Should().BeEquivalentTo(bilinmeyen.Errors);
    }

    [Fact]
    public async Task Pasif_hesabin_bilgileriyle_giris_dogrulama_postasi_tetikler()
    {
        ByUsername(Account(isActive: false, isDeleted: false));

        var result = await Login();

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(403);
        result.Data.Should().BeNull("hesap açılana kadar oturum kurulmaz");

        _activation.Verify(a => a.IssueAsync(7, "Login", "203.0.113.9", "Firefox", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Pasif_hesapta_yanlis_parola_posta_tetiklemez()
    {
        ByUsername(Account(isActive: false, isDeleted: false));

        var result = await Login("yanlis-parola");

        result.StatusCode.Should().Be(401);
        VerifyNoActivation();
        _throttle.Verify(t => t.RegisterFailure("deneme"), Times.Once);
    }

    [Fact]
    public async Task Etkin_hesabin_girisi_etkilenmez()
    {
        ByUsername(Account(isActive: true, isDeleted: false));

        var result = await Login();

        result.Success.Should().BeTrue();
        result.Data!.Token.Should().NotBeNullOrWhiteSpace();
        VerifyNoActivation();
    }

    [Fact]
    public async Task Silinmis_hesabin_bilgileriyle_kayit_temiz_reddedilir()
    {
        ByUsername(Account(isActive: false, isDeleted: true));

        var result = await Register();

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(400);
        result.InternalMessage.Should().Contain("silinmiş hesap");
        _created.Should().BeEmpty();
        VerifyNoActivation();
    }

    [Fact]
    public async Task Pasif_hesabin_bilgileriyle_kayit_yeni_satir_acmaz_posta_tetikler()
    {
        ByUsername(Account(isActive: false, isDeleted: false));

        var result = await Register();

        result.IsFailure.Should().BeTrue();
        _created.Should().BeEmpty("kullanıcı kendi hesabını geri istiyor, ikincisini değil");
        _activation.Verify(a => a.IssueAsync(7, "Register", "203.0.113.9", "Firefox", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Kayitta_adres_uzerinden_eslesen_pasif_hesap_da_tetikler()
    {
        ByUsername(null);
        ByEmail(Account(isActive: false, isDeleted: false, id: 11));

        await Register();

        _created.Should().BeEmpty();
        _activation.Verify(a => a.IssueAsync(11, "Register", It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Kayitta_uc_durum_da_ayni_metni_alir()
    {
        var metinler = new List<string>();

        foreach (var owner in new[]
                 {
                     Account(isActive: true, isDeleted: false),
                     Account(isActive: false, isDeleted: false),
                     Account(isActive: false, isDeleted: true)
                 })
        {
            ByUsername(owner);
            metinler.Add((await Register()).Errors[0]);
        }

        metinler.Distinct().Should().ContainSingle("hangi durumun tetiklendiği yanıttan okunamamalı");
    }

    [Fact]
    public async Task Aktivasyon_gonderilemezse_kullaniciya_ayni_metin_doner()
    {
        ByUsername(Account(isActive: false, isDeleted: false));
        _activation.Setup(a => a.IssueAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail("Hesaba bağlı bir e-posta adresi yok.", "#7 adressiz."));

        var result = await Login();

        result.StatusCode.Should().Be(403);
        result.Errors[0].Should().Contain("iletişim formundan");
        result.InternalMessage.Should().Contain("#7 adressiz.");
    }

    [Fact]
    public async Task Yeni_kullanici_kaydi_calismaya_devam_eder()
    {
        ByUsername(null);
        ByEmail(null);

        var result = await Register();

        result.Success.Should().BeTrue();
        _created.Should().ContainSingle();
        VerifyNoActivation();
    }

    [Fact]
    public async Task Basarisiz_robot_dogrulamasi_kayda_uyari_olarak_dusuyor()
    {
        _turnstile.Setup(t => t.VerifyAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await Register();

        result.StatusCode.Should().Be(400);
        _logged.Should().ContainSingle(l => l.Level == "Warning" && l.Message!.Contains("robot doğrulaması başarısız"),
            "reddedilen istek sunucuda iz bırakmazsa böyle bir şikâyet geldiğinde bakılacak hiçbir kayıt olmaz");
    }

    [Fact]
    public async Task Hatali_parola_kayda_uyari_olarak_dusuyor()
    {
        ByUsername(Account(isActive: true, isDeleted: false));

        var result = await Login("yanlis-parola");

        result.StatusCode.Should().Be(401);
        _logged.Should().ContainSingle(l => l.Level == "Warning" && l.Message!.Contains("parola hatalı"),
            "arka arkaya hatalı denemeler ancak kayda düşerse fark edilebilir");
    }

    [Fact]
    public async Task Basarili_giris_kayda_dusuyor()
    {
        ByUsername(Account(isActive: true, isDeleted: false));

        var result = await Login();

        result.Success.Should().BeTrue();
        _logged.Should().ContainSingle(l => l.Level == "Information" && l.Message!.Contains("Giriş yapıldı"),
            "denetim kaydı yalnızca reddedilenleri değil, kimin ne zaman girdiğini de taşımalı");
    }

    [Fact]
    public async Task Kayda_yazilan_hicbir_satir_parola_tasimaz()
    {
        ByUsername(Account(isActive: true, isDeleted: false));

        await Login("yanlis-parola");
        await Login();

        _logged.Should().NotBeEmpty();
        _logged.Should().OnlyContain(
            l => !l.Message!.Contains(Password) && !l.Message!.Contains("yanlis-parola"),
            "denetim kaydı yönetim panelinde okunabiliyor; denenen parolayı oraya yazmak onu ikinci bir yerde saklamak olur");
    }

    [Theory]
    [InlineData("Ksa1!")]
    [InlineData("yalnizcakucuk1!")]
    [InlineData("Rakamsiz!Parola")]
    [InlineData("Sembolsuz1Parola")]
    public async Task Politikayi_gecmeyen_parolayla_kayit_reddedilir(string parola)
    {
        ByUsername(null);
        ByEmail(null);

        var result = await _sut.RegisterAsync(new RegisterDto
        {
            Username = "deneme",
            Email = "deneme@ornek.test",
            Password = parola,
            AcceptAgreement = true
        }, "203.0.113.9", "Firefox");

        result.IsFailure.Should().BeTrue("parola kuralı sunucuda çalışmazsa istemci doğrulaması atlanabilir");
        _created.Should().BeEmpty();
    }
}
