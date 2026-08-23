using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Business.Services.Concrete;
using FurkanTural_Domain.Entities;
using Moq;

namespace FurkanTural_Business.Tests;

public class AccountActivationServiceTests
{
    private static readonly DateTime Now = new(2026, 8, 24, 12, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IRepository<AccountActivation>> _activations = new();
    private readonly List<AccountActivation> _added = [];
    private readonly AccountActivationService _sut;

    public AccountActivationServiceTests()
    {
        var clock = new Mock<IClock>();
        clock.SetupGet(c => c.UtcNow).Returns(Now);

        _activations.Setup(r => r.AddAsync(It.IsAny<AccountActivation>(), It.IsAny<CancellationToken>()))
            .Callback<AccountActivation, CancellationToken>((a, _) => _added.Add(a))
            .Returns(Task.CompletedTask);

        _uow.SetupGet(u => u.Users).Returns(_users.Object);
        _uow.SetupGet(u => u.AccountActivations).Returns(_activations.Object);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _sut = new AccountActivationService(_uow.Object, clock.Object);
    }

    private static string Sha256(string value)
        => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private void UserIs(User? user)
        => _users.Setup(r => r.GetByIdForAdminAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);

    private void ActivationIs(AccountActivation? activation)
        => _activations
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<AccountActivation, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(activation);

    private static User Passive(int id = 7) => new() { Id = id, Username = "deneme", IsActive = false, IsDeleted = false };

    private static AccountActivation Valid(string token, int userId = 7) => new()
    {
        Id = 1,
        UserId = userId,
        TokenHash = Sha256(token),
        ExpiresAt = Now.AddHours(1)
    };

    [Fact]
    public async Task Uretilen_jetonun_kendisi_degil_ozeti_saklanir()
    {
        UserIs(Passive());

        var result = await _sut.IssueAsync(7, "Login", "203.0.113.9", "Firefox");

        result.Success.Should().BeTrue();
        var token = result.Data!;
        _added.Should().ContainSingle();
        _added[0].TokenHash.Should().NotBe(token);
        _added[0].TokenHash.Should().Be(Sha256(token));
    }

    [Fact]
    public async Task Jeton_baglantida_tasinabilir_karakterlerden_olusur()
    {
        UserIs(Passive());

        var token = (await _sut.IssueAsync(7, "Login", null, null)).Data!;

        token.Should().MatchRegex("^[A-Za-z0-9_-]+$");
        token.Length.Should().BeGreaterThan(40);
    }

    [Fact]
    public async Task Ard_arda_uretilen_jetonlar_ayni_olmaz()
    {
        UserIs(Passive());

        var first = (await _sut.IssueAsync(7, "Login", null, null)).Data;
        var second = (await _sut.IssueAsync(7, "Login", null, null)).Data;

        first.Should().NotBe(second);
    }

    [Fact]
    public async Task Gecerlilik_suresi_yirmi_dort_saattir()
    {
        UserIs(Passive());

        await _sut.IssueAsync(7, "Register", null, null);

        _added[0].ExpiresAt.Should().Be(Now.AddHours(24));
    }

    [Fact]
    public async Task Istek_bilgileri_kolon_genisligine_kirpilir()
    {
        UserIs(Passive());

        await _sut.IssueAsync(7, new string('t', 80), new string('i', 60), new string('u', 400));

        _added[0].TriggerSource!.Length.Should().Be(50);
        _added[0].RequestIpAddress!.Length.Should().Be(45);
        _added[0].RequestUserAgent!.Length.Should().Be(300);
    }

    [Fact]
    public async Task Silinmis_hesap_icin_jeton_uretilmez()
    {
        UserIs(new User { Id = 7, IsDeleted = true });

        var result = await _sut.IssueAsync(7, "Login", null, null);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(404);
        _added.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Bos_jeton_reddedilir(string? token)
        => (await _sut.ConsumeAsync(token)).IsFailure.Should().BeTrue();

    [Fact]
    public async Task Taninmayan_jeton_reddedilir()
    {
        ActivationIs(null);

        var result = await _sut.ConsumeAsync("yok-boyle-bir-jeton");

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Should().Be("Doğrulama bağlantısı geçersiz.");
    }

    [Fact]
    public async Task Harcanmis_jeton_ikinci_kez_calismaz()
    {
        var activation = Valid("jeton");
        activation.ConsumedAt = Now.AddMinutes(-5);
        ActivationIs(activation);

        var result = await _sut.ConsumeAsync("jeton");

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(410);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Suresi_gecmis_jeton_calismaz()
    {
        var activation = Valid("jeton");
        activation.ExpiresAt = Now.AddSeconds(-1);
        ActivationIs(activation);

        var result = await _sut.ConsumeAsync("jeton");

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(410);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Silinmis_hesap_jetonla_acilamaz()
    {
        ActivationIs(Valid("jeton"));
        UserIs(new User { Id = 7, IsDeleted = true, IsActive = false });

        var result = await _sut.ConsumeAsync("jeton");

        result.IsFailure.Should().BeTrue();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Gecerli_jeton_hesabi_aktiflestirir_ve_jetonu_harcar()
    {
        var activation = Valid("jeton");
        var user = Passive();
        ActivationIs(activation);
        UserIs(user);

        var result = await _sut.ConsumeAsync("jeton");

        result.Success.Should().BeTrue();
        user.IsActive.Should().BeTrue();
        activation.ConsumedAt.Should().Be(Now);
        _users.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Zaten_aktif_hesapta_jeton_harcanir_kullanici_yazilmaz()
    {
        var activation = Valid("jeton");
        var user = Passive();
        user.IsActive = true;
        ActivationIs(activation);
        UserIs(user);

        var result = await _sut.ConsumeAsync("jeton");

        result.Success.Should().BeTrue();
        activation.ConsumedAt.Should().Be(Now);
        _users.Verify(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
