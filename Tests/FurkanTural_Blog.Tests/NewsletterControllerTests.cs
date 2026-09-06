using FluentAssertions;
using FurkanTural_Blog.Controllers;
using FurkanTural_Blog.Models;
using FurkanTural_Blog.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FurkanTural_Blog.Tests;

/// <summary>Bülten formunun sunucu tarafı. Tarayıcının <c>type="email" required</c> denetimi burada yok sayılır: istek elle de kurulabildiği için sunucu doğrulaması tek başına ayakta durmalıdır.</summary>
public class NewsletterControllerTests
{
    private static NewsletterViewModel Model(string? email = "okur@example.invalid", string? trap = null)
        => new() { Email = email, Website = trap };

    private static NewsletterViewModel ResultOf(IActionResult action)
        => ((ViewResult)action).Model.As<NewsletterViewModel>();

    [Fact]
    public async Task Tuzak_alan_doluysa_istek_api_ye_hic_cikmaz()
    {
        var client = new Mock<INewsletterClient>(MockBehavior.Strict);
        var controller = new NewsletterController(client.Object);

        var result = ResultOf(await controller.Index(Model(trap: "http://spam.example"), default));

        client.Verify(c => c.SubscribeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Tuzaga_dusen_gonderim_basarili_gorunur()
    {
        var controller = new NewsletterController(Mock.Of<INewsletterClient>());

        var result = ResultOf(await controller.Index(Model(trap: "x"), default));

        result.Succeeded.Should().BeTrue();
        result.ResultMessage.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Gecersiz_model_api_ye_gitmeden_geri_doner()
    {
        var client = new Mock<INewsletterClient>(MockBehavior.Strict);
        var controller = new NewsletterController(client.Object);
        controller.ModelState.AddModelError("Email", "Geçerli bir e-posta adresi girin.");

        var result = ResultOf(await controller.Index(Model("abc"), default));

        client.Verify(c => c.SubscribeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Adres_bosluklari_kirpilarak_gonderilir()
    {
        var client = new Mock<INewsletterClient>();
        client.Setup(c => c.SubscribeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new NewsletterOutcome(true, "Abonelik başarıyla tamamlandı."));

        await new NewsletterController(client.Object).Index(Model("  okur@example.invalid  "), default);

        client.Verify(c => c.SubscribeAsync("okur@example.invalid", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Basarili_kayitta_adres_formdan_silinir()
    {
        var client = new Mock<INewsletterClient>();
        client.Setup(c => c.SubscribeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new NewsletterOutcome(true, "Abonelik başarıyla tamamlandı."));

        var result = ResultOf(await new NewsletterController(client.Object).Index(Model(), default));

        result.Email.Should().BeNull();
    }

    [Fact]
    public async Task Api_nin_kendi_metni_oldugu_gibi_gosterilir()
    {
        var client = new Mock<INewsletterClient>();
        client.Setup(c => c.SubscribeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new NewsletterOutcome(false, "Bu e-posta adresi zaten abone listesinde."));

        var result = ResultOf(await new NewsletterController(client.Object).Index(Model(), default));

        result.Succeeded.Should().BeFalse();
        result.ResultMessage.Should().Be("Bu e-posta adresi zaten abone listesinde.");
    }

    [Fact]
    public async Task Api_metin_vermezse_yedek_metin_kullanilir()
    {
        var client = new Mock<INewsletterClient>();
        client.Setup(c => c.SubscribeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new NewsletterOutcome(false, null));

        var result = ResultOf(await new NewsletterController(client.Object).Index(Model(), default));

        result.ResultMessage.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Basarisiz_kayitta_adres_formda_kalir()
    {
        var client = new Mock<INewsletterClient>();
        client.Setup(c => c.SubscribeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new NewsletterOutcome(false, null));

        var result = ResultOf(await new NewsletterController(client.Object).Index(Model(), default));

        result.Email.Should().Be("okur@example.invalid");
    }

    [Fact]
    public void Ilk_acilista_form_bos_ve_sonucsuz_gelir()
    {
        var result = ((ViewResult)new NewsletterController(Mock.Of<INewsletterClient>()).Index())
            .Model.As<NewsletterViewModel>();

        result.Submitted.Should().BeFalse();
        result.Email.Should().BeNull();
    }
}
