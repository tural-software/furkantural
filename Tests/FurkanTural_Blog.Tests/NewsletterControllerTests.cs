using FluentAssertions;
using FurkanTural_Blog;
using FurkanTural_Blog.Controllers;
using FurkanTural_Blog.Models;
using FurkanTural_Blog.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FurkanTural_Blog.Tests;

/// <summary>Bülten formunun sunucu tarafı. Tarayıcının <c>type="email" required</c> denetimi burada yok sayılır: istek elle de kurulabildiği için sunucu doğrulaması tek başına ayakta durmalıdır.<para>Abonelik artık çift onaylıdır — form listeye yazmaz, yalnızca doğrulama bağlantısı gönderilmesini ister.</para></summary>
public class NewsletterControllerTests
{
    private static NewsletterController Build(INewsletterClient client, string? siteKey = "site-anahtari")
    {
        var config = new Mock<IAppConfigService>();
        config.Setup(c => c.GetTurnstileSiteKeyAsync(It.IsAny<CancellationToken>())).ReturnsAsync(siteKey);
        return new NewsletterController(client, config.Object);
    }

    private static NewsletterViewModel Model(string? email = "okur@example.invalid", string? trap = null, string? turnstile = "bot-jetonu")
        => new() { Email = email, Website = trap, TurnstileToken = turnstile };

    private static NewsletterViewModel ResultOf(IActionResult action)
        => ((ViewResult)action).Model.As<NewsletterViewModel>();

    private static NewsletterTokenViewModel TokenResultOf(IActionResult action)
        => ((ViewResult)action).Model.As<NewsletterTokenViewModel>();

    private static Mock<INewsletterClient> ClientReturning(bool succeeded, string? message)
    {
        var client = new Mock<INewsletterClient>();
        client.Setup(c => c.SubscribeAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new NewsletterOutcome(succeeded, message));
        client.Setup(c => c.ConfirmAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new NewsletterOutcome(succeeded, message));
        client.Setup(c => c.UnsubscribeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new NewsletterOutcome(succeeded, message));
        client.Setup(c => c.RequestUnsubscribeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new NewsletterOutcome(succeeded, message));
        return client;
    }

    // ── Abonelik formu ────────────────────────────────────────────────────────

    [Fact]
    public async Task Tuzak_alan_doluysa_istek_api_ye_hic_cikmaz()
    {
        var client = new Mock<INewsletterClient>(MockBehavior.Strict);

        var result = ResultOf(await Build(client.Object).Index(Model(trap: "http://spam.example"), default));

        client.Verify(c => c.SubscribeAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        result.Succeeded.Should().BeTrue("bota tuzağa düştüğünü söylemek tuzağı işe yaramaz hâle getirir");
    }

    [Fact]
    public async Task Gecersiz_model_api_ye_gitmeden_geri_doner()
    {
        var client = new Mock<INewsletterClient>(MockBehavior.Strict);
        var controller = Build(client.Object);
        controller.ModelState.AddModelError("Email", "Geçerli bir e-posta adresi girin.");

        var result = ResultOf(await controller.Index(Model("abc"), default));

        client.Verify(c => c.SubscribeAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        result.Succeeded.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Bot_jetonu_yoksa_istek_aga_cikmaz(string? turnstile)
    {
        var client = new Mock<INewsletterClient>(MockBehavior.Strict);

        var result = ResultOf(await Build(client.Object).Index(Model(turnstile: turnstile), default));

        client.Verify(c => c.SubscribeAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        result.Succeeded.Should().BeFalse();
        result.ResultMessage.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Adres_ve_bot_jetonu_birlikte_gonderilir()
    {
        var client = ClientReturning(true, "Doğrulama bağlantısı gönderildi.");

        await Build(client.Object).Index(Model("  okur@example.invalid  ", turnstile: "bot-jetonu"), default);

        client.Verify(c => c.SubscribeAsync("okur@example.invalid", "bot-jetonu", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Basarili_kayitta_adres_formdan_silinir()
    {
        var result = ResultOf(await Build(ClientReturning(true, "Doğrulama bağlantısı gönderildi.").Object).Index(Model(), default));

        result.Email.Should().BeNull();
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Api_nin_kendi_metni_oldugu_gibi_gosterilir()
    {
        var result = ResultOf(await Build(ClientReturning(false, "Bot doğrulaması başarısız.").Object).Index(Model(), default));

        result.Succeeded.Should().BeFalse();
        result.ResultMessage.Should().Be("Bot doğrulaması başarısız.");
    }

    [Fact]
    public async Task Api_metin_vermezse_yedek_metin_kullanilir()
    {
        var result = ResultOf(await Build(ClientReturning(false, null).Object).Index(Model(), default));

        result.ResultMessage.Should().NotBeNullOrWhiteSpace();
        result.Email.Should().Be("okur@example.invalid", "başarısız kayıtta adres formda kalmalı");
    }

    [Fact]
    public async Task Ilk_acilista_form_bos_ve_sonucsuz_gelir()
    {
        var result = ResultOf(await Build(Mock.Of<INewsletterClient>()).Index(default(CancellationToken)));

        result.Submitted.Should().BeFalse();
        result.Email.Should().BeNull();
    }

    [Fact]
    public async Task Site_anahtari_goruntuye_gecirilir()
    {
        var controller = Build(Mock.Of<INewsletterClient>(), "anahtar-42");

        await controller.Index(default(CancellationToken));

        ((string?)controller.ViewBag.TurnstileSiteKey).Should().Be("anahtar-42");
    }

    // ── Onay bağlantısı ───────────────────────────────────────────────────────

    [Fact]
    public async Task Onay_baglantisi_jetonu_harcar()
    {
        var client = ClientReturning(true, "Aboneliğiniz doğrulandı.");

        var result = TokenResultOf(await Build(client.Object).Confirm("jeton", default));

        result.Succeeded.Should().BeTrue();
        result.Message.Should().Be("Aboneliğiniz doğrulandı.");
        client.Verify(c => c.ConfirmAsync("jeton", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Jetonsuz_gelen_ziyaretciye_hata_degil_yonerge_gosterilir(string? token)
    {
        var client = new Mock<INewsletterClient>(MockBehavior.Strict);

        var result = TokenResultOf(await Build(client.Object).Confirm(token, default));

        result.TokenMissing.Should().BeTrue();
        client.Verify(c => c.ConfirmAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Çıkış ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Cikis_baglantisi_jetonla_gelirse_aboneligi_bitirir()
    {
        var client = ClientReturning(true, "Aboneliğiniz iptal edildi.");

        var result = TokenResultOf(await Build(client.Object).Unsubscribe("jeton", default));

        result.Succeeded.Should().BeTrue();
        client.Verify(c => c.UnsubscribeAsync("jeton", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cikis_sayfasi_jetonsuz_gelirse_form_gosterir()
    {
        var client = new Mock<INewsletterClient>(MockBehavior.Strict);

        var view = (ViewResult)await Build(client.Object).Unsubscribe((string?)null, default);

        view.ViewName.Should().Be("Unsubscribe");
        view.Model.Should().BeOfType<NewsletterViewModel>();
        client.Verify(c => c.UnsubscribeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Cikis_formu_listeden_dusurmez_yalnizca_baglanti_ister()
    {
        var client = ClientReturning(true, "Adres listemizdeyse çıkış bağlantısını gönderdik.");

        var result = ResultOf(await Build(client.Object).Unsubscribe(Model(turnstile: null), default));

        result.Succeeded.Should().BeTrue();
        client.Verify(c => c.RequestUnsubscribeAsync("okur@example.invalid", It.IsAny<CancellationToken>()), Times.Once);
        client.Verify(c => c.UnsubscribeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Cikis_formunda_gecersiz_adres_api_ye_gitmez()
    {
        var client = new Mock<INewsletterClient>(MockBehavior.Strict);
        var controller = Build(client.Object);
        controller.ModelState.AddModelError("Email", "Geçerli bir e-posta adresi girin.");

        var result = ResultOf(await controller.Unsubscribe(Model("abc"), default));

        result.Succeeded.Should().BeFalse();
        client.Verify(c => c.RequestUnsubscribeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
