using System.Net;
using System.Security.Claims;
using FluentAssertions;
using FurkanTural_API.Middlewares;
using Microsoft.AspNetCore.Http;

namespace FurkanTural_API.Tests;

public class ForwardedClientMiddlewareTests
{
    private const string SolutionMarker = "FurkanTural.slnx";
    private const string VisitorIp = "198.51.100.7";
    private const string VisitorAgent = "Mozilla/5.0 (Ziyaretci)";
    private static readonly IPAddress ServerIp = IPAddress.Parse("203.0.113.10");

    private static DefaultHttpContext Request(ClaimsPrincipal user, string? ip = VisitorIp, string? userAgent = VisitorAgent)
    {
        var context = new DefaultHttpContext { User = user };
        context.Connection.RemoteIpAddress = ServerIp;
        if (ip is not null)
            context.Request.Headers[ForwardedClientMiddleware.IpHeader] = ip;
        if (userAgent is not null)
            context.Request.Headers[ForwardedClientMiddleware.UserAgentHeader] = userAgent;
        return context;
    }

    private const string KeyId = "anahtar-kimligi";

    private static ClaimsPrincipal Token(string? appSource = null, string? role = null, string? keyId = null)
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

    private static Task Run(HttpContext context)
        => new ForwardedClientMiddleware(_ => Task.CompletedTask).InvokeAsync(context);

    [Theory]
    [InlineData("Portfolio")]
    [InlineData("Blog")]
    [InlineData("Chat")]
    [InlineData("Admin")]
    public async Task Sitelerimizin_jetonuyla_gelen_ziyaretci_bilgisi_kullanilir(string app)
    {
        var context = Request(Token(appSource: app, role: "Visitor", keyId: KeyId));

        await Run(context);

        context.Connection.RemoteIpAddress.Should().Be(IPAddress.Parse(VisitorIp),
            "app-token her zaman Visitor rolüyle üretilir; ziyaretçi bilgisini yalnızca sunucudan sunucuya " +
            "konuşan bu jeton taşıyabilir");
        context.Request.Headers.UserAgent.ToString().Should().Be(VisitorAgent);
    }

    [Fact]
    public async Task Yonetici_rolu_tek_basina_ziyaretci_bilgisi_yazdirmaz()
    {
        var context = Request(Token(role: "Admin"));

        await Run(context);

        context.Connection.RemoteIpAddress.Should().Be(ServerIp,
            "yönetici jetonunun yalnızca panelde durduğu varsayımı yanlıştı: jeton API'ye doğrudan da " +
            "sunulabilir, o yüzden rol tek başına başlık yazma yetkisi vermemeli");
    }

    [Theory]
    [InlineData("Chat")]
    [InlineData("Admin")]
    public async Task Anahtar_kimligi_olmayan_Visitor_jetonu_ziyaretci_bilgisi_yazdirmaz(string app)
    {
        var context = Request(Token(appSource: app, role: "Visitor"));

        await Run(context);

        context.Connection.RemoteIpAddress.Should().Be(ServerIp,
            "Visitor veri tabanında da bir roldür; bu role atanmış bir üyenin kendi giriş jetonu da Visitor ve " +
            "app_source taşır, uygulama jetonunu ondan ayıran tek şey anahtar kimliğidir");
    }

    [Fact]
    public async Task Jetonsuz_istekte_basliklar_yok_sayilir()
    {
        var context = Request(new ClaimsPrincipal(new ClaimsIdentity()));

        await Run(context);

        context.Connection.RemoteIpAddress.Should().Be(ServerIp,
            "jetonsuz bir istemci başlığı kendisi yazabilir; kabul edilseydi herkes IP uydururdu");
        context.Request.Headers.UserAgent.ToString().Should().BeEmpty();
    }

    [Theory]
    [InlineData("Mobile", null)]
    [InlineData(null, "User")]
    [InlineData(null, "Visitor")]
    [InlineData("Chat", "User")]
    [InlineData("Chat", "Admin")]
    [InlineData("Admin", "User")]
    public async Task Taninmayan_uygulamanin_ya_da_rolun_basliklari_yok_sayilir(string? app, string? role)
    {
        var context = Request(Token(appSource: app, role: role));

        await Run(context);

        context.Connection.RemoteIpAddress.Should().Be(ServerIp,
            "app_source girişte istemcinin gönderdiği gövdeden üretilir; kullanıcı jetonu da onu taşıyabildiği " +
            "için tek başına yetmez, yoksa üye kendi IP'sini uydurup giriş kilidini boşa düşürür");
    }

    [Theory]
    [InlineData("bilinmiyor")]
    [InlineData("12")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Gecersiz_ip_baglanti_adresini_degistirmez(string ip)
    {
        var context = Request(Token(appSource: "Blog", role: "Visitor", keyId: KeyId), ip: ip);

        await Run(context);

        context.Connection.RemoteIpAddress.Should().Be(ServerIp);
    }

    [Fact]
    public async Task IPv6_adresi_kabul_edilir()
    {
        var context = Request(Token(appSource: "Chat", role: "Visitor", keyId: KeyId), ip: "2001:db8::7");

        await Run(context);

        context.Connection.RemoteIpAddress.Should().Be(IPAddress.Parse("2001:db8::7"));
    }

    [Fact]
    public async Task Tarayici_bilgisi_olmadan_mevcut_deger_korunur()
    {
        var context = Request(Token(appSource: "Portfolio", role: "Visitor", keyId: KeyId), userAgent: null);
        context.Request.Headers.UserAgent = "sunucu-istemcisi";

        await Run(context);

        context.Request.Headers.UserAgent.ToString().Should().Be("sunucu-istemcisi");
    }

    [Fact]
    public async Task Uzun_tarayici_bilgisi_kirpilir()
    {
        var context = Request(Token(appSource: "Blog", role: "Visitor", keyId: KeyId), userAgent: new string('a', 2000));

        await Run(context);

        context.Request.Headers.UserAgent.ToString().Length.Should().Be(ForwardedClientMiddleware.MaxUserAgentLength);
    }

    [Fact]
    public void Ara_katman_kimlik_dogrulamadan_sonra_yetkilendirmeden_once_calisir()
    {
        var program = File.ReadAllText(Path.Combine(FindSolutionRoot(), "Web", "FurkanTural_API", "Program.cs"));

        var authentication = program.IndexOf("app.UseAuthentication();", StringComparison.Ordinal);
        var forwarded = program.IndexOf("app.UseMiddleware<ForwardedClientMiddleware>();", StringComparison.Ordinal);
        var authorization = program.IndexOf("app.UseAuthorization();", StringComparison.Ordinal);

        authentication.Should().BeGreaterThan(-1);
        forwarded.Should().BeGreaterThan(authentication,
            "kimlik doğrulanmadan jetonun hangi uygulamaya ait olduğu bilinemez ve başlıklar hep yok sayılır");
        authorization.Should().BeGreaterThan(forwarded);
    }

    [Theory]
    [InlineData("Presentation/FurkanTural_Blog/ClientForwardingHandler.cs")]
    [InlineData("Presentation/FurkanTural_Portfolio/ClientForwardingHandler.cs")]
    [InlineData("Presentation/FurkanTural_Chat/ClientForwardingHandler.cs")]
    [InlineData("Presentation/FurkanTural_Admin/Services/ClientForwardingHandler.cs")]
    public void Siteler_API_ile_ayni_baslik_adlarini_kullanir(string relativePath)
    {
        var source = File.ReadAllText(Path.Combine([FindSolutionRoot(), .. relativePath.Split('/')]));

        source.Should().Contain($"\"{ForwardedClientMiddleware.IpHeader}\"");
        source.Should().Contain($"\"{ForwardedClientMiddleware.UserAgentHeader}\"");
    }

    [Theory]
    [InlineData("FurkanTural_Blog", false)]
    [InlineData("FurkanTural_Portfolio", false)]
    [InlineData("FurkanTural_Chat", true)]
    [InlineData("FurkanTural_Admin", true)]
    public void Siteler_gercek_ziyaretci_ipsini_cozer_ve_API_isteklerine_ekler(string project, bool hasBff)
    {
        var program = File.ReadAllText(Path.Combine(FindSolutionRoot(), "Presentation", project, "Program.cs"));

        program.Should().Contain("app.UseRealClientIp(builder.Configuration);",
            "Cloudflare arkasında bağlantı adresi ziyaretçi değil kenar sunucusudur");
        program.Should().Contain("AddHttpMessageHandler<ClientForwardingHandler>()",
            "sunucudan yapılan API çağrıları ziyaretçinin bilgisini taşımalı");
        if (hasBff)
            program.Should().Contain("ClientForwarding.Apply(ctx.HttpContext, ctx.ProxyRequest.Headers);",
                "tarayıcının BFF üzerinden yaptığı çağrılar da ziyaretçinin bilgisini taşımalı");
    }

    private static string FindSolutionRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, SolutionMarker)))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            $"'{SolutionMarker}' bulunamadı; arama '{AppContext.BaseDirectory}' dizininden yukarı doğru yapıldı.");
    }
}
