using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using FurkanTural_Application.DTOs.Auth;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Settings;
using FurkanTural_Business.Helpers;
using FurkanTural_Business.Services.Concrete;
using FurkanTural_Domain.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Moq;

namespace FurkanTural_Business.Tests;

/// <summary>Uygulama jetonu 365 gün yaşıyor ve iptal edilemiyordu; sızmış tek bir site jetonu bir yıl boyunca geçerli kalırdı. Jeton artık saatlerle ölçülür ve onu üreten anahtara bağlıdır.</summary>
public class AppTokenKeyBindingTests
{
    private const string Secret = "birim-testleri-icin-yeterince-uzun-bir-imza-anahtari";
    private static readonly DateTime Now = new(2026, 9, 16, 12, 0, 0, DateTimeKind.Utc);

    private static AuthService Build(AppTokenSettings settings)
        => new(
            Mock.Of<IUnitOfWork>(), Mock.Of<IPasswordHasher>(),
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["JwtSettings:Secret"] = Secret }).Build(),
            Options.Create(settings), Mock.Of<ITurnstileVerifier>(), Mock.Of<ILoginThrottle>(), Mock.Of<IAccountActivationService>(),
            new ActivityLogger(Mock.Of<ILogService>(), Mock.Of<IHttpContextAccessor>(), Mock.Of<IClock>()),
            Mock.Of<IClock>(c => c.UtcNow == Now));

    private static AppTokenSettings Settings(string key = "blog-anahtari", int hours = 6)
        => new() { ExpiryHours = hours, Apps = [new AppRegistration { AppName = "Blog", AppKey = key }] };

    [Fact]
    public async Task Jeton_saatlerle_olculur_ve_anahtar_kimligini_tasir()
    {
        var settings = Settings();

        var result = await Build(settings).GenerateAppTokenAsync(new AppTokenRequestDto { AppName = "Blog", AppKey = "blog-anahtari" });

        result.Data!.ExpiresAt.Should().Be(Now.AddHours(6));
        var keyId = new JwtSecurityTokenHandler().ReadJwtToken(result.Data.Token).Claims
            .Single(c => c.Type == ClaimDefinitions.AppKeyId).Value;
        AppKeyIds.Matches(Secret, settings, "Blog", keyId).Should().BeTrue();
    }

    [Theory]
    [InlineData(0, 2)]
    [InlineData(1, 2)]
    [InlineData(8760, 24)]
    public async Task Sure_iki_ile_yirmi_dort_saat_arasina_sikistirilir(int configured, int expected)
    {
        var result = await Build(Settings(hours: configured))
            .GenerateAppTokenAsync(new AppTokenRequestDto { AppName = "Blog", AppKey = "blog-anahtari" });

        result.Data!.ExpiresAt.Should().Be(Now.AddHours(expected));
    }

    [Fact]
    public void Anahtar_dondurulunce_eski_jetonun_kimligi_eslesmez()
    {
        var eskiKimlik = AppKeyIds.For(Secret, "Blog", "eski-anahtar");

        AppKeyIds.Matches(Secret, Settings(key: "yeni-anahtar"), "Blog", eskiKimlik).Should().BeFalse(
            "anahtar değişince dağıtılmış jetonlar süresini beklemeden geçersizleşmeli");
    }

    [Fact]
    public void Donus_sirasinda_iki_anahtar_birlikte_gecerlidir()
    {
        var settings = new AppTokenSettings
        {
            Apps = [new AppRegistration { AppName = "Blog", AppKey = "eski" }, new AppRegistration { AppName = "Blog", AppKey = "yeni" }]
        };

        AppKeyIds.Matches(Secret, settings, "Blog", AppKeyIds.For(Secret, "Blog", "eski")).Should().BeTrue();
        AppKeyIds.Matches(Secret, settings, "Blog", AppKeyIds.For(Secret, "Blog", "yeni")).Should().BeTrue();
    }

    [Fact]
    public void Listeden_cikarilan_uygulamanin_jetonu_eslesmez()
        => AppKeyIds.Matches(Secret, new AppTokenSettings(), "Blog", AppKeyIds.For(Secret, "Blog", "blog-anahtari")).Should().BeFalse();

    [Fact]
    public void Baska_uygulamanin_kimligi_bu_uygulamaya_gecmez()
    {
        var settings = new AppTokenSettings
        {
            Apps = [new AppRegistration { AppName = "Blog", AppKey = "ortak" }, new AppRegistration { AppName = "Chat", AppKey = "ortak" }]
        };

        AppKeyIds.Matches(Secret, settings, "Chat", AppKeyIds.For(Secret, "Blog", "ortak")).Should().BeFalse(
            "kimlik uygulama adını da kapsar; aynı anahtar iki uygulamada kullanılsa bile jeton birinden ötekine taşınamaz");
    }

    [Fact]
    public void Kimlik_imza_sirri_olmadan_uretilemez()
        => AppKeyIds.For("baska-sir", "Blog", "blog-anahtari").Should().NotBe(AppKeyIds.For(Secret, "Blog", "blog-anahtari"),
            "jetonun içi okunabilir; anahtarın düz bir özeti orada dursa kısa anahtarlar çevrimdışı denenebilirdi");
}
