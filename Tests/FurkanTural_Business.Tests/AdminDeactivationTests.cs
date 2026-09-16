using System.Linq.Expressions;
using FluentAssertions;
using FurkanTural_Application.DTOs.Auth;
using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.DTOs.Log;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Settings;
using FurkanTural_Application.Wrappers;
using FurkanTural_Business.Helpers;
using FurkanTural_Business.Services.Concrete;
using FurkanTural_Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace FurkanTural_Business.Tests;

/// <summary>Yöneticinin kapattığı hesap, kullanıcının doğru parolayla giriş yapıp postadaki bağlantıya tıklamasıyla yeniden açılabiliyordu; yasak ile kullanıcının kendi kapatması arasında ayrım yoktu.</summary>
public class AdminDeactivationTests
{
    private static readonly DateTime Now = new(2026, 9, 16, 12, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IRepository<PushSubscription>> _subscriptions = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly IClock _clock = Mock.Of<IClock>(c => c.UtcNow == Now);
    private List<PushSubscription> _deleted = [];

    public AdminDeactivationTests()
    {
        _uow.SetupGet(u => u.Users).Returns(_users.Object);
        _uow.SetupGet(u => u.PushSubscriptions).Returns(_subscriptions.Object);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _uow.Setup(u => u.TryConsumeTokenAsync<AccountActivation>(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        _subscriptions.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<PushSubscription, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<PushSubscription, bool>> p, CancellationToken _) =>
                new[] { new PushSubscription { Id = 1, UserId = 7 }, new PushSubscription { Id = 2, UserId = 9 } }.Where(p.Compile()));
        _subscriptions.Setup(r => r.DeleteRangeAsync(It.IsAny<IEnumerable<PushSubscription>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PushSubscription>, CancellationToken>((items, _) => _deleted = items.ToList())
            .Returns(Task.CompletedTask);

        _hasher.Setup(h => h.IsHashed(It.IsAny<string?>())).Returns(true);
        _hasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
    }

    private User Account(bool isActive = true, bool byAdmin = false)
    {
        var user = new User
        {
            Id = 7, Username = "deneme", Email = "deneme@ornek.test", Password = "ozet", RoleId = 2,
            IsActive = isActive, DeactivatedByAdmin = byAdmin, SecurityStamp = "damga",
            DeactivatedAt = isActive ? null : Now.AddDays(-1)
        };
        _users.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _users.Setup(r => r.GetByIdForAdminAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _users.Setup(r => r.GetByUsernameForAdminAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);
        return user;
    }

    private UserService Users()
        => new(_uow.Object, _hasher.Object, new ActivityLogger(Mock.Of<ILogService>(), Mock.Of<IHttpContextAccessor>(), _clock),
            Mock.Of<IUserFriendService>(), _clock);

    [Fact]
    public async Task Yonetici_kapatinca_yasak_ve_tarih_isaretlenir_push_abonelikleri_silinir()
    {
        var user = Account();

        await Users().ToggleActiveAsync(7, updatedBy: 1);

        user.IsActive.Should().BeFalse();
        user.DeactivatedByAdmin.Should().BeTrue();
        user.DeactivatedAt.Should().Be(Now, "saklama süresi hesabın kapatıldığı andan işler");
        _deleted.Should().ContainSingle(s => s.UserId == 7, "yasaklanan hesabın cihazlarına bildirim gitmeye devam etmemeli");
    }

    [Fact]
    public async Task Yonetici_yeniden_acinca_isaretler_temizlenir()
    {
        var user = Account(isActive: false, byAdmin: true);

        await Users().ToggleActiveAsync(7, updatedBy: 1);

        user.IsActive.Should().BeTrue();
        user.DeactivatedByAdmin.Should().BeFalse();
        user.DeactivatedAt.Should().BeNull();
    }

    [Fact]
    public async Task Kullanicinin_kendi_kapatmasi_yasak_sayilmaz()
    {
        var user = Account();

        await Users().DeactivateMyAccountAsync(7, "parola");

        user.DeactivatedByAdmin.Should().BeFalse();
        user.DeactivatedAt.Should().Be(Now);
    }

    [Fact]
    public async Task Toplu_kapatma_yasak_olarak_isaretler_ve_bildirimleri_siler()
    {
        var user = Account();
        _users.Setup(r => r.GetAllForAdminAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { user }.AsEnumerable());

        await Users().BulkAsync(BulkAction.Deactivate, [7], 1);

        user.DeactivatedByAdmin.Should().BeTrue();
        _deleted.Should().ContainSingle(s => s.UserId == 7);
    }

    private AccountActivationService Activations(List<AccountActivation> stored)
    {
        var activations = new Mock<IRepository<AccountActivation>>();
        activations.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AccountActivation, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<AccountActivation, bool>> p, CancellationToken _) => stored.FirstOrDefault(p.Compile()));
        _uow.SetupGet(u => u.AccountActivations).Returns(activations.Object);

        return new AccountActivationService(_uow.Object, Mock.Of<IMailSender>(),
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Activation:LandingUrl"] = "https://ornek.test/a" }).Build(),
            NullLogger<AccountActivationService>.Instance, _clock);
    }

    [Fact]
    public async Task Yasakli_hesap_icin_aktivasyon_uretilmez()
    {
        Account(isActive: false, byAdmin: true);

        var result = await Activations([]).IssueAsync(7, "Login", null, null);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task Yasaktan_once_uretilmis_baglanti_hesabi_acmaz()
    {
        var user = Account(isActive: false, byAdmin: true);
        var token = "bekleyen-jeton";
        var hash = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));

        var result = await Activations([new AccountActivation { Id = 3, UserId = 7, TokenHash = hash, ExpiresAt = Now.AddHours(1) }])
            .ConsumeAsync(token);

        result.IsFailure.Should().BeTrue("yasak konmadan önce postalanmış bir bağlantı yasağı delmemeli");
        user.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Kullanicinin_kendi_kapattigi_hesap_baglantiyla_acilir_ve_isaretler_temizlenir()
    {
        var user = Account(isActive: false, byAdmin: false);
        var token = "jeton";
        var hash = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));

        var result = await Activations([new AccountActivation { Id = 3, UserId = 7, TokenHash = hash, ExpiresAt = Now.AddHours(1) }])
            .ConsumeAsync(token);

        result.Success.Should().BeTrue();
        user.IsActive.Should().BeTrue();
        user.DeactivatedAt.Should().BeNull();
    }

    [Fact]
    public async Task Yasakli_hesabin_girisi_aktivasyon_gondermeden_reddedilir()
    {
        Account(isActive: false, byAdmin: true);
        var activation = new Mock<IAccountActivationService>();
        var throttle = new Mock<ILoginThrottle>();
        throttle.Setup(t => t.GetRemainingLockout(It.IsAny<string?>())).Returns((TimeSpan?)null);
        var logService = new Mock<ILogService>();
        logService.Setup(l => l.CreateAsync(It.IsAny<CreateLogDto>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result<LogDto>.Ok(new LogDto()));

        var sut = new AuthService(_uow.Object, _hasher.Object,
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["JwtSettings:Secret"] = "birim-testleri-icin-yeterince-uzun-bir-imza-anahtari" }).Build(),
            Options.Create(new AppTokenSettings()), Mock.Of<ITurnstileVerifier>(), throttle.Object, activation.Object,
            new ActivityLogger(logService.Object, Mock.Of<IHttpContextAccessor>(), _clock), _clock);

        var result = await sut.LoginAsync(new LoginDto { Username = "deneme", Password = "parola" }, null, null, "Admin");

        result.StatusCode.Should().Be(403);
        result.Errors[0].Should().Contain("yönetici");
        activation.Verify(a => a.IssueAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
