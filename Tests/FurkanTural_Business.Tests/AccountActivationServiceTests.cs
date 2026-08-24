using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using FurkanTural_Application.DTOs.Mail;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using FurkanTural_Business.Services.Concrete;
using FurkanTural_Domain.Constants;
using FurkanTural_Domain.Entities;
using Microsoft.Extensions.Configuration;
using Moq;

namespace FurkanTural_Business.Tests;

public class AccountActivationServiceTests
{
    private static readonly DateTime Now = new(2026, 8, 24, 12, 0, 0, DateTimeKind.Utc);
    private const string Landing = "https://sohbet.ornek.test/Account/Activate";

    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IRepository<AccountActivation>> _activations = new();
    private readonly Mock<IMailSender> _mail = new();
    private readonly List<AccountActivation> _added = [];
    private readonly List<AccountActivationMailDto> _sent = [];
    private readonly Dictionary<string, string?> _settings = new() { ["Activation:LandingUrl"] = Landing };
    private AccountActivationService _sut;

    public AccountActivationServiceTests()
    {
        var clock = new Mock<IClock>();
        clock.SetupGet(c => c.UtcNow).Returns(Now);

        _activations.Setup(r => r.AddAsync(It.IsAny<AccountActivation>(), It.IsAny<CancellationToken>()))
            .Callback<AccountActivation, CancellationToken>((a, _) => _added.Add(a))
            .Returns(Task.CompletedTask);

        _mail.Setup(m => m.SendAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Callback<string, string?, string?, object, CancellationToken>((_, _, _, p, _) => _sent.Add((AccountActivationMailDto)p))
            .ReturnsAsync(Result.Ok());

        _uow.SetupGet(u => u.Users).Returns(_users.Object);
        _uow.SetupGet(u => u.AccountActivations).Returns(_activations.Object);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _sut = Build();
    }

    private AccountActivationService Build()
        => new(_uow.Object, _mail.Object,
            new ConfigurationBuilder().AddInMemoryCollection(_settings).Build(),
            Mock.Of<IClock>(c => c.UtcNow == Now));

    private static string Sha256(string value)
        => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static string TokenFrom(AccountActivationMailDto dto)
        => Uri.UnescapeDataString(dto.ActivationUrl!.Split("?token=")[1]);

    private void UserIs(User? user)
        => _users.Setup(r => r.GetByIdForAdminAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);

    private void ActivationIs(AccountActivation? activation)
        => _activations
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<AccountActivation, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(activation);

    private static User Passive(int id = 7) => new()
    {
        Id = id,
        Username = "deneme",
        DisplayName = "Deneme Kullanıcı",
        Email = "deneme@ornek.test",
        IsActive = false,
        IsDeleted = false
    };

    private static AccountActivation Valid(string token, int userId = 7) => new()
    {
        Id = 1,
        UserId = userId,
        TokenHash = Sha256(token),
        ExpiresAt = Now.AddHours(1)
    };

    [Fact]
    public async Task Postaya_giden_jetonun_kendisi_degil_ozeti_saklanir()
    {
        UserIs(Passive());

        var result = await _sut.IssueAsync(7, "Login", "203.0.113.9", "Firefox");

        result.Success.Should().BeTrue();
        _sent.Should().ContainSingle();
        _added.Should().ContainSingle();

        var token = TokenFrom(_sent[0]);
        _added[0].TokenHash.Should().NotBe(token);
        _added[0].TokenHash.Should().Be(Sha256(token));
    }

    [Fact]
    public async Task Jeton_hicbir_donus_degerinde_yer_almaz()
    {
        UserIs(Passive());

        var result = await _sut.IssueAsync(7, "Login", null, null);
        var token = TokenFrom(_sent[0]);

        result.Message.Should().NotContain(token);
        result.InternalMessage.Should().NotContain(token);
        string.Join(" ", result.Errors).Should().NotContain(token);
    }

    [Fact]
    public async Task Baglanti_yapilandirilan_adrese_kurulur()
    {
        UserIs(Passive());

        await _sut.IssueAsync(7, "Login", null, null);

        _sent[0].ActivationUrl.Should().StartWith(Landing + "?token=");
        TokenFrom(_sent[0]).Should().MatchRegex("^[A-Za-z0-9_-]+$");
    }

    [Fact]
    public async Task Ard_arda_uretilen_jetonlar_ayni_olmaz()
    {
        UserIs(Passive());

        await _sut.IssueAsync(7, "Login", null, null);
        await _sut.IssueAsync(7, "Login", null, null);

        TokenFrom(_sent[0]).Should().NotBe(TokenFrom(_sent[1]));
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
        UserIs(new User { Id = 7, IsDeleted = true, Email = "x@ornek.test" });

        var result = await _sut.IssueAsync(7, "Login", null, null);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(404);
        _added.Should().BeEmpty();
        _sent.Should().BeEmpty();
    }

    [Fact]
    public async Task Adresi_olmayan_hesap_icin_jeton_uretilmez()
    {
        var user = Passive();
        user.Email = null;
        UserIs(user);

        var result = await _sut.IssueAsync(7, "Login", null, null);

        result.IsFailure.Should().BeTrue();
        _added.Should().BeEmpty();
        _sent.Should().BeEmpty();
    }

    [Fact]
    public async Task Adres_yapilandirilmamissa_jeton_harcanmaz()
    {
        _settings["Activation:LandingUrl"] = null;
        _sut = Build();
        UserIs(Passive());

        var result = await _sut.IssueAsync(7, "Login", null, null);

        result.IsFailure.Should().BeTrue();
        _added.Should().BeEmpty("çalışmayan bir bağlantı yollamaktansa hiç yollamamak gerekir");
        _sent.Should().BeEmpty();
    }

    [Fact]
    public async Task Posta_gonderilemezse_sonuc_basarisiz_doner()
    {
        UserIs(Passive());
        _mail.Setup(m => m.SendAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail("Posta gönderilemedi.", "SMTP kapalı.", 502));

        var result = await _sut.IssueAsync(7, "Login", null, null);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(502);
        result.InternalMessage.Should().Be("SMTP kapalı.");
    }

    [Fact]
    public async Task Aktivasyon_postasi_kendi_turuyle_ve_hesabin_adresine_gonderilir()
    {
        UserIs(Passive());

        await _sut.IssueAsync(7, "Login", null, null);

        _mail.Verify(m => m.SendAsync(
            MailTemplateDefinitions.AccountActivation,
            AppSourceDefinitions.Chat,
            "deneme@ornek.test",
            It.IsAny<AccountActivationMailDto>(),
            It.IsAny<CancellationToken>()), Times.Once);
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
