using System.Security.Claims;
using FluentAssertions;
using FurkanTural_Application.DTOs.Auth;
using FurkanTural_Application.DTOs.User;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using FurkanTural_API.Controllers;
using FurkanTural_API.Models.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FurkanTural_API.Tests;

/// <summary>Giriş ucunun, isteği yapan ön-yüzü nereden okuduğu. Uygulama jetonu yalnızca <c>AppTokens:Apps</c> altındaki anahtarla üretilir ve her zaman Visitor rolü taşır; bu yüzden ön-yüz kimliği olarak kullanılabilecek tek kaynak odur. Kullanıcı jetonu da <c>app_source</c> taşıyabildiği için rol denetimi olmadan okumak, herhangi bir üyenin kendini ön-yüz gibi göstermesine izin verirdi.</summary>
public class LoginTrustedAppSourceTests
{
    private static ClaimsPrincipal Token(string? appSource, string? role, string? keyId = null)
    {
        var claims = new List<Claim>();
        if (appSource is not null)
            claims.Add(new Claim("app_source", appSource));
        if (role is not null)
            claims.Add(new Claim(ClaimTypes.Role, role));
        if (keyId is not null)
            claims.Add(new Claim("app_kid", keyId));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
    }

    private static async Task<string?> TrustedSourceSeenByService(ClaimsPrincipal user)
    {
        string? captured = null;

        var authService = new Mock<IAuthService>();
        authService
            .Setup(a => a.LoginAsync(It.IsAny<LoginDto>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<LoginDto, string?, string?, string?, CancellationToken>((_, _, _, trusted, _) => captured = trusted)
            .ReturnsAsync(Result<LoginResultDto>.Ok(new LoginResultDto()));

        var controller = new AuthController(authService.Object, Mock.Of<IAccountActivationService>())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } }
        };

        await controller.Login(
            new LoginRequest { Username = "deneme", Password = "parola", AppSource = "Admin" },
            CancellationToken.None);

        return captured;
    }

    [Theory]
    [InlineData("Chat")]
    [InlineData("Admin")]
    public async Task Uygulama_jetonunun_kaynagi_servise_gecer(string app)
        => (await TrustedSourceSeenByService(Token(app, "Visitor", "anahtar-kimligi"))).Should().Be(app);

    [Theory]
    [InlineData("Chat")]
    [InlineData("Admin")]
    public async Task Anahtar_kimligi_olmayan_Visitor_jetonu_on_yuz_kimligi_sayilmaz(string app)
        => (await TrustedSourceSeenByService(Token(app, "Visitor"))).Should().BeNull(
            "Visitor rolüne atanmış bir üyenin giriş jetonu da Visitor ve app_source taşır; kimliği yalnızca " +
            "uygulama jetonuna basılan anahtar kimliği kanıtlar");

    [Theory]
    [InlineData("User")]
    [InlineData("Admin")]
    public async Task Kullanici_jetonu_on_yuz_kimligi_sayilmaz(string role)
        => (await TrustedSourceSeenByService(Token("Chat", role))).Should().BeNull(
            "app_source girişte istemcinin gönderdiği gövdeden üretilebiliyor; rol denetimi olmadan okunsaydı " +
            "herhangi bir üye kendini ön-yüz gibi gösterip robot doğrulamasını atlardı");

    [Fact]
    public async Task Jetonsuz_istek_taninmayan_cagirandir()
        => (await TrustedSourceSeenByService(new ClaimsPrincipal(new ClaimsIdentity()))).Should().BeNull();

    [Fact]
    public async Task Kaynagi_olmayan_uygulama_jetonu_kimlik_saglamaz()
        => (await TrustedSourceSeenByService(Token(null, "Visitor"))).Should().BeNull();

    [Fact]
    public async Task Govdede_bildirilen_uygulama_adi_kimlik_yerine_gecmez()
        => (await TrustedSourceSeenByService(Token(null, null))).Should().NotBe("Admin",
            "istek gövdesindeki AppSource istemcinin yazdığı bir alandır; kimlik olarak okunsaydı doğrulamadan " +
            "kaçmak için orayı doldurmak yeterdi");
}
