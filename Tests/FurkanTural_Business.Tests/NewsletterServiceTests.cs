using System.Text.RegularExpressions;
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

/// <summary>Çift onaylı abonelik akışı. Jeton düz hâliyle servisten hiç çıkmadığı için test onu <b>giden postanın içinden</b> alır — üretimdeki tek yol da budur; jetonu bir dönüş değerinden okumak, gerçekte var olmayan bir kapıyı sınamak olurdu.<para>Uçların hiçbiri bir adresin listede olup olmadığını ele vermemelidir; testlerin bir bölümü tam olarak bunu, yani iki durumun ayırt edilemezliğini sınar.</para></summary>
public class NewsletterServiceTests
{
    private const string Email = "okur@ornek.test";
    private const string ConfirmUrl = "https://blog.test/bulten/onay";
    private const string UnsubscribeUrl = "https://blog.test/bulten/cikis";

    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ISubscriberRepository> _subscribers = new();
    private readonly Mock<IRepository<SubscriberVerification>> _verifications = new();
    private readonly Mock<IMailSender> _mail = new();
    private readonly Mock<ITurnstileVerifier> _turnstile = new();

    private readonly List<Subscriber> _added = [];
    private readonly List<Subscriber> _restored = [];
    private readonly List<Subscriber> _softDeleted = [];
    private readonly List<SubscriberVerification> _issued = [];
    private readonly List<(string Type, string? To, object Payload)> _sent = [];
    private readonly ActivityJournal _journal = new();

    private Result _outcome = Result.Ok();
    private SubscriberVerification? _pending;
    private SubscriberVerification? _stored;
    private Subscriber? _byId;

    private static readonly DateTime Now = new(2026, 9, 6, 12, 0, 0, DateTimeKind.Utc);

    private readonly NewsletterService _sut;

    public NewsletterServiceTests()
    {
        _turnstile.Setup(t => t.VerifyAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _subscribers.Setup(r => r.AddAsync(It.IsAny<Subscriber>(), It.IsAny<CancellationToken>()))
            .Callback<Subscriber, CancellationToken>((s, _) => { s.Id = 7; _added.Add(s); _byId ??= s; })
            .Returns(Task.CompletedTask);
        _subscribers.Setup(r => r.RestoreAsync(It.IsAny<Subscriber>(), It.IsAny<CancellationToken>()))
            .Callback<Subscriber, CancellationToken>((s, _) => { s.IsDeleted = false; s.IsActive = true; _restored.Add(s); })
            .Returns(Task.CompletedTask);
        _subscribers.Setup(r => r.SoftDeleteAsync(It.IsAny<Subscriber>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .Callback<Subscriber, int?, CancellationToken>((s, _, _) => { s.IsDeleted = true; _softDeleted.Add(s); })
            .Returns(Task.CompletedTask);
        _subscribers.Setup(r => r.UpdateAsync(It.IsAny<Subscriber>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _subscribers.Setup(r => r.GetByIdForAdminAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => _byId);

        _verifications.Setup(r => r.AddAsync(It.IsAny<SubscriberVerification>(), It.IsAny<CancellationToken>()))
            .Callback<SubscriberVerification, CancellationToken>((v, _) => { v.CreatedAt = Now; _issued.Add(v); _stored = v; })
            .Returns(Task.CompletedTask);
        _verifications.Setup(r => r.UpdateAsync(It.IsAny<SubscriberVerification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _verifications.Setup(r => r.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<SubscriberVerification, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((System.Linq.Expressions.Expression<Func<SubscriberVerification, bool>> p, CancellationToken _) =>
            {
                var compiled = p.Compile();
                if (_pending is not null && compiled(_pending)) return _pending;
                return _stored is not null && compiled(_stored) ? _stored : null;
            });

        _mail.Setup(m => m.SendAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Callback<string, string?, string?, object, CancellationToken>((type, _, to, payload, _) => { _sent.Add((type, to, payload)); _journal.Mail(); })
            .ReturnsAsync(() => _outcome);

        _uow.SetupGet(u => u.Subscribers).Returns(_subscribers.Object);
        _uow.SetupGet(u => u.SubscriberVerifications).Returns(_verifications.Object);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _uow.Setup(u => u.TryConsumeTokenAsync<SubscriberVerification>(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Newsletter:ConfirmUrl"] = ConfirmUrl,
            ["Newsletter:UnsubscribeUrl"] = UnsubscribeUrl
        }).Build();

        var clock = Mock.Of<IClock>(c => c.UtcNow == Now);
        _sut = new NewsletterService(_uow.Object, _mail.Object, _turnstile.Object, config, _journal.Logger(clock), clock);
    }

    private void RowIs(Subscriber? row)
    {
        _byId = row;
        _subscribers.Setup(r => r.GetByEmailForAdminAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(row);
    }

    private static Subscriber Row(bool isActive = true, bool isDeleted = false, DateTime? confirmedAt = null)
        => new() { Id = 7, Email = Email, IsActive = isActive, IsDeleted = isDeleted, ConfirmedAt = confirmedAt };

    /// <summary>Jetonu giden postanın bağlantısından çıkarır. Üretimde de tek yol budur.</summary>
    private string TokenFromMail()
    {
        _sent.Should().NotBeEmpty("jeton yalnızca giden postanın içinde bulunur");
        var url = _sent[^1].Payload switch
        {
            NewsletterConfirmMailDto c => c.ConfirmUrl,
            NewsletterUnsubscribeMailDto u => u.UnsubscribeUrl,
            _ => null
        };
        var match = Regex.Match(url ?? "", @"token=([^&]+)");
        match.Success.Should().BeTrue("bağlantı jetonu taşımalı");
        return Uri.UnescapeDataString(match.Groups[1].Value);
    }

    // ── Abonelik ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Yeni_adres_dogrulanmamis_acilir_ve_posta_gider()
    {
        RowIs(null);

        var result = await _sut.SubscribeAsync(Email, "jeton", null, null);

        result.Success.Should().BeTrue();
        _added.Should().ContainSingle();
        _added[0].ConfirmedAt.Should().BeNull("bağlantıya tıklanmadan abonelik başlamaz");
        _sent.Should().ContainSingle();
        _sent[0].Type.Should().Be(MailTemplateDefinitions.NewsletterConfirm);
        _sent[0].To.Should().Be(Email);
    }

    [Fact]
    public async Task Jeton_kayda_duz_haliyle_yazilmaz()
    {
        RowIs(null);

        await _sut.SubscribeAsync(Email, "jeton", null, null);

        var token = TokenFromMail();
        _issued.Should().ContainSingle();
        _issued[0].TokenHash.Should().NotBe(token, "veri tabanına jetonun kendisi değil türevi yazılır");
        _issued[0].TokenHash.Should().NotContain(token);
        _issued[0].Purpose.Should().Be(SubscriberVerificationPurposes.Confirm);
        _issued[0].ExpiresAt.Should().Be(Now.AddHours(24));
    }

    [Fact]
    public async Task Silinmis_adres_geri_acilir_ve_dogrulama_sifirlanir()
    {
        RowIs(Row(isActive: false, isDeleted: true, confirmedAt: Now.AddDays(-30)));

        var result = await _sut.SubscribeAsync(Email, "jeton", null, null);

        result.Success.Should().BeTrue();
        _restored.Should().ContainSingle();
        _restored[0].ConfirmedAt.Should().BeNull("geri açılan kayıt yeniden doğrulanmalı");
        _added.Should().BeEmpty("duran satır varken yeni satır açılmaz");
    }

    [Fact]
    public async Task Zaten_dogrulanmis_adrese_ikinci_posta_gitmez()
    {
        RowIs(Row(confirmedAt: Now.AddDays(-1)));

        var result = await _sut.SubscribeAsync(Email, "jeton", null, null);

        result.Success.Should().BeTrue();
        _sent.Should().BeEmpty();
        _issued.Should().BeEmpty();
    }

    [Fact]
    public async Task Kayitli_ve_kayitsiz_adres_ayni_yaniti_verir()
    {
        RowIs(Row(confirmedAt: Now.AddDays(-1)));
        var kayitli = await _sut.SubscribeAsync(Email, "jeton", null, null);

        RowIs(null);
        var kayitsiz = await _sut.SubscribeAsync(Email, "jeton", null, null);

        kayitli.Success.Should().Be(kayitsiz.Success);
        kayitli.Message.Should().Be(kayitsiz.Message, "yanıt farklılaşırsa uç, kimin abone olduğunu sınayan bir araca döner");
    }

    [Fact]
    public async Task Bot_dogrulamasi_gecmezse_kayit_acilmaz()
    {
        _turnstile.Setup(t => t.VerifyAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        RowIs(null);

        var result = await _sut.SubscribeAsync(Email, "kotu", null, null);

        result.IsFailure.Should().BeTrue();
        _added.Should().BeEmpty();
        _sent.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("bosluk li@ornek.test")]
    public async Task Gecersiz_adres_reddedilir(string? email)
    {
        RowIs(null);

        (await _sut.SubscribeAsync(email, "jeton", null, null)).IsFailure.Should().BeTrue();
        _added.Should().BeEmpty();
    }

    [Fact]
    public async Task Adres_kucuk_harfe_indirilir()
    {
        RowIs(null);

        await _sut.SubscribeAsync("  OKUR@Ornek.TEST  ", "jeton", null, null);

        _added.Should().ContainSingle();
        _added[0].Email.Should().Be(Email);
    }

    [Fact]
    public async Task Bekleyen_baglanti_varken_ikinci_posta_gonderilmez()
    {
        RowIs(Row());
        _pending = new SubscriberVerification
        {
            SubscriberId = 7,
            Purpose = SubscriberVerificationPurposes.Confirm,
            CreatedAt = Now.AddMinutes(-1),
            ExpiresAt = Now.AddHours(23)
        };

        var result = await _sut.SubscribeAsync(Email, "jeton", null, null);

        result.Success.Should().BeTrue();
        _sent.Should().BeEmpty("aksi hâlde uç, istediği adrese arka arkaya posta yollatabilen bir mekanizmaya dönerdi");
        _journal.Events.Should().Equal("log:Information");
    }

    [Fact]
    public async Task Baglanti_adresi_yapilandirilmamissa_jeton_harcanmaz()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        var clock = Mock.Of<IClock>(c => c.UtcNow == Now);
        var sut = new NewsletterService(_uow.Object, _mail.Object, _turnstile.Object, config, _journal.Logger(clock), clock);
        RowIs(null);

        var result = await sut.SubscribeAsync(Email, "jeton", null, null);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(500);
        _issued.Should().BeEmpty();
        _sent.Should().BeEmpty();
        _journal.Events.Should().Equal("log:Error");
    }

    [Fact]
    public async Task Kayit_postanin_sonucu_belli_olduktan_sonra_yazilir()
    {
        RowIs(null);

        await _sut.SubscribeAsync(Email, "jeton", null, null);

        _journal.Events.Should().Equal("mail", "log:Information");
        _journal.Logs[0].Message.Should().Contain("Bülten kaydı açıldı").And.Contain("gönderildi");
    }

    [Fact]
    public async Task Gonderilemeyen_posta_hata_olarak_kayda_gecer()
    {
        RowIs(null);
        _outcome = Result.Fail("Posta gönderilemedi.", "SMTP hatası (newsletter-confirm): 535 Invalid Username or Password", 502);

        var result = await _sut.SubscribeAsync(Email, "jeton", null, null);

        result.IsFailure.Should().BeTrue();
        _journal.Events.Should().Equal("mail", "log:Error");
        _journal.Logs[0].Message.Should().Contain("gönderilemedi").And.Contain("535");
    }

    [Fact]
    public async Task Gonderilemeyen_postanin_baglantisi_yeni_denemeyi_bekletmez()
    {
        RowIs(Row());
        _outcome = Result.Fail("Posta gönderilemedi.", "SMTP hatası", 502);
        await _sut.SubscribeAsync(Email, "jeton", null, null);

        _outcome = Result.Ok();
        var result = await _sut.SubscribeAsync(Email, "jeton", null, null);

        result.Success.Should().BeTrue();
        _sent.Should().HaveCount(2, "hiç ulaşmamış bir bağlantı soğuma penceresini doldurursa ikinci deneme posta göndermeden 'gönderdik' der");
        _issued[0].ExpiresAt.Should().Be(Now);
    }

    [Fact]
    public async Task Gonderilmeyen_posta_da_kayda_gecer()
    {
        RowIs(Row(confirmedAt: Now.AddDays(-1)));

        await _sut.SubscribeAsync(Email, "jeton", null, null);

        _journal.Events.Should().Equal("log:Information");
        _journal.Logs[0].Message.Should().Contain("gönderilmedi").And.Contain("zaten doğrulanmış");
    }

    // ── Onay ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Postadaki_baglanti_aboneligi_acar()
    {
        RowIs(null);
        await _sut.SubscribeAsync(Email, "jeton", null, null);
        var token = TokenFromMail();

        var result = await _sut.ConfirmAsync(token);

        result.Success.Should().BeTrue();
        _byId!.ConfirmedAt.Should().Be(Now);
        _stored!.ConsumedAt.Should().Be(Now);
    }

    [Fact]
    public async Task Ayni_baglanti_ikinci_kez_calismaz()
    {
        RowIs(null);
        await _sut.SubscribeAsync(Email, "jeton", null, null);
        var token = TokenFromMail();
        await _sut.ConfirmAsync(token);

        var result = await _sut.ConfirmAsync(token);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(410);
    }

    [Fact]
    public async Task Suresi_gecmis_baglanti_reddedilir()
    {
        RowIs(null);
        await _sut.SubscribeAsync(Email, "jeton", null, null);
        var token = TokenFromMail();
        _stored!.ExpiresAt = Now.AddHours(-1);

        var result = await _sut.ConfirmAsync(token);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(410);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("uydurma-jeton")]
    public async Task Gecersiz_jeton_reddedilir(string? token)
        => (await _sut.ConfirmAsync(token)).IsFailure.Should().BeTrue();

    [Fact]
    public async Task Cikis_jetonu_aboneligi_onaylayamaz()
    {
        RowIs(Row(confirmedAt: Now.AddDays(-1)));
        await _sut.RequestUnsubscribeAsync(Email, null, null);
        var token = TokenFromMail();

        var result = await _sut.ConfirmAsync(token);

        result.IsFailure.Should().BeTrue("jeton amacının dışında kullanılamamalı");
    }

    // ── Çıkış ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Cikis_istegi_yalnizca_baglanti_gonderir()
    {
        RowIs(Row(confirmedAt: Now.AddDays(-1)));

        var result = await _sut.RequestUnsubscribeAsync(Email, null, null);

        result.Success.Should().BeTrue();
        _softDeleted.Should().BeEmpty("istek tek başına listeden düşürmemeli");
        _sent.Should().ContainSingle();
        _sent[0].Type.Should().Be(MailTemplateDefinitions.NewsletterUnsubscribe);
    }

    [Fact]
    public async Task Listede_olmayan_adres_icin_de_ayni_yanit_doner()
    {
        RowIs(Row(confirmedAt: Now.AddDays(-1)));
        var listede = await _sut.RequestUnsubscribeAsync(Email, null, null);

        RowIs(null);
        var listede_degil = await _sut.RequestUnsubscribeAsync(Email, null, null);

        listede.Success.Should().Be(listede_degil.Success);
        listede.Message.Should().Be(listede_degil.Message);
        _sent.Should().ContainSingle("listede olmayan adrese posta gitmemeli");
    }

    [Fact]
    public async Task Postadaki_baglanti_aboneligi_listeden_duserir()
    {
        RowIs(Row(confirmedAt: Now.AddDays(-1)));
        await _sut.RequestUnsubscribeAsync(Email, null, null);
        var token = TokenFromMail();

        var result = await _sut.UnsubscribeAsync(token);

        result.Success.Should().BeTrue();
        _softDeleted.Should().ContainSingle();
        _stored!.ConsumedAt.Should().Be(Now);
    }

    [Fact]
    public async Task Onay_jetonu_listeden_duseremez()
    {
        RowIs(null);
        await _sut.SubscribeAsync(Email, "jeton", null, null);
        var token = TokenFromMail();

        var result = await _sut.UnsubscribeAsync(token);

        result.IsFailure.Should().BeTrue();
        _softDeleted.Should().BeEmpty();
    }

    [Fact]
    public async Task Cikis_bot_dogrulamasi_istemez()
    {
        _turnstile.Setup(t => t.VerifyAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        RowIs(Row(confirmedAt: Now.AddDays(-1)));

        var result = await _sut.RequestUnsubscribeAsync(Email, null, null);

        result.Success.Should().BeTrue("çıkışı zorlaştırmak, izinli listeyi kirletmenin yolu olurdu");
    }

    [Fact]
    public async Task Cikista_aboneye_ait_bekleyen_butun_baglantilar_harcanir()
    {
        RowIs(Row(confirmedAt: Now.AddDays(-1)));
        await _sut.RequestUnsubscribeAsync(Email, null, null);
        var token = TokenFromMail();

        var result = await _sut.UnsubscribeAsync(token);

        result.Success.Should().BeTrue();
        _uow.Verify(u => u.ConsumePendingSubscriberVerificationsAsync(7, Now, It.IsAny<CancellationToken>()), Times.Once,
            "bültenlere gömülü çıkış bağlantıları yıllarca geçerli; harcanmazsa kişi yeniden abone olduğunda eski bir posta yeni aboneliği iptal ederdi");
    }

    [Fact]
    public async Task Yoneticinin_listeden_cikardigi_adres_formdan_geri_acilmaz()
    {
        var row = Row(isActive: false, isDeleted: true);
        row.DeletedBy = 1;
        RowIs(row);

        var result = await _sut.SubscribeAsync(Email, "jeton", null, null);

        result.Success.Should().BeTrue("yanıt adresin durumunu ele vermemeli");
        _restored.Should().BeEmpty();
        _sent.Should().BeEmpty();
    }

    [Fact]
    public async Task Yoneticinin_listeden_cikardigi_adres_onay_baglantisiyla_geri_acilmaz()
    {
        RowIs(null);
        await _sut.SubscribeAsync(Email, "jeton", null, null);
        var token = TokenFromMail();
        _byId!.IsDeleted = true;
        _byId.DeletedBy = 1;

        var result = await _sut.ConfirmAsync(token);

        result.IsFailure.Should().BeTrue();
        _restored.Should().BeEmpty();
        _byId.ConfirmedAt.Should().BeNull();
    }

    [Fact]
    public async Task Ayni_anda_harcanan_jeton_ikinci_istekte_aboneligi_baslatmaz()
    {
        RowIs(null);
        await _sut.SubscribeAsync(Email, "jeton", null, null);
        var token = TokenFromMail();
        _uow.Setup(u => u.TryConsumeTokenAsync<SubscriberVerification>(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _sut.ConfirmAsync(token);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(410);
        _byId!.ConfirmedAt.Should().BeNull("jetonu harcayan istek bu değilse aboneliği başlatan da o değildir");
    }
}
