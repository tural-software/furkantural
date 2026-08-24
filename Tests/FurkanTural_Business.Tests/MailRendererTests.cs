using FluentAssertions;
using FurkanTural_Application.DTOs.Mail;
using FurkanTural_Business.Services.Concrete;
using FurkanTural_Domain.Constants;
using Microsoft.Extensions.Logging.Abstractions;

namespace FurkanTural_Business.Tests;

public class MailRendererTests
{
    private readonly MailRenderer _sut = new(NullLogger<MailRenderer>.Instance);

    [Fact]
    public void Yer_tutucular_gövde_dtosundaki_adlarla_degistirilir()
    {
        var html = "<p>Merhaba {{FullName}}, adresin {{Email}}</p>";

        var result = _sut.Render(html, new ContactOwnerMailDto { FullName = "Ada", Email = "ada@ornek.test" });

        result.Should().Be("<p>Merhaba Ada, adresin ada@ornek.test</p>");
    }

    [Fact]
    public void Karsiligi_olmayan_yer_tutucu_bosa_indirilir()
    {
        var result = _sut.Render("<p>{{FullName}} / {{BoyleBirAlanYok}}</p>",
            new ContactOwnerMailDto { FullName = "Ada" });

        result.Should().Be("<p>Ada / </p>");
        result.Should().NotContain("{{");
    }

    [Fact]
    public void DolduruLmayan_alan_bos_metne_doner_null_yazilmaz()
    {
        var result = _sut.Render("[{{Message}}]", new ContactOwnerMailDto { FullName = "Ada" });

        result.Should().Be("[]");
    }

    [Fact]
    public void Eslesme_buyuk_kucuk_harfe_duyarlidir()
    {
        var result = _sut.Render("{{fullname}}", new ContactOwnerMailDto { FullName = "Ada" });

        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Bos_sablon_bos_metne_doner(string? template)
        => _sut.Render(template, new ContactOwnerMailDto()).Should().BeEmpty();

    [Fact]
    public void Konu_da_gövdeyle_ayni_yolu_kullanir()
    {
        var result = _sut.Render("Yeni İletişim Mesajı - {{FullName}}", new ContactOwnerMailDto { FullName = "Ada" });

        result.Should().Be("Yeni İletişim Mesajı - Ada");
    }

    [Fact]
    public void Tohumlanan_her_turun_bir_govde_dtosu_vardir()
    {
        string[] seeded =
        [
            MailTemplateDefinitions.ContactOwner,
            MailTemplateDefinitions.ContactUser,
            MailTemplateDefinitions.AccountActivation
        ];

        foreach (var code in seeded)
            MailPayloads.PlaceholdersOf(code).Should().NotBeEmpty($"{code} türünü gönderen bir kod yolu var");
    }

    [Fact]
    public void Panelden_eklenen_tur_icin_yer_tutucu_listesi_bostur()
        => MailPayloads.PlaceholdersOf("PanelinEklediğiTür").Should().BeEmpty();

    [Fact]
    public void Iletisim_sablonlarinin_bekledigi_alanlar_dtolarda_karsilanir()
    {
        var owner = MailPayloads.PlaceholdersOf(MailTemplateDefinitions.ContactOwner);
        owner.Should().BeEquivalentTo(
            ["FullName", "Email", "Message", "CreatedAt", "IpAddress", "Browser", "FormPageUrl"]);

        var user = MailPayloads.PlaceholdersOf(MailTemplateDefinitions.ContactUser);
        user.Should().BeEquivalentTo(
            ["FullName", "Email", "Message", "CreatedAt", "CurrentYear", "ContactEmail", "LinkedInUrl", "GitHubUrl", "InstagramUrl"]);
    }
}
