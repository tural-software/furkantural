using FluentAssertions;

namespace FurkanTural_Chat.Tests.Services;

public class BffOriginGuardTests
{
    private const string SolutionMarker = "FurkanTural.slnx";

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

    private static string Program() => File.ReadAllText(Path.Combine(
        FindSolutionRoot(), "Presentation", "FurkanTural_Chat", "Program.cs"));

    [Fact]
    public void Vekil_yabanci_kokenden_gelen_istegi_reddeder()
    {
        var program = Program();

        program.Should().Contain("Sec-Fetch-Site",
            "blog ve portfolyo chat ile aynı site sayılır; SameSite=Lax çerezi o kökenlerden gelen isteğe de " +
            "ekler, dolayısıyla oturumu jetona çeviren vekilin kendi köken denetimi olmalı");

        program.Should().Contain("StatusCodes.Status403Forbidden",
            "denetim isteği geçirip yalnız günlüğe yazarsa saldırı durmaz");

        program.Should().Contain("originUri.Host",
            "şema karşılaştırmaya girerse Cloudflare TLS'i sonlandırıp origin'e http konuştuğunda " +
            "her istek reddedilir; ayrım için ana bilgisayar adı yeterli");
    }

    [Fact]
    public void Koken_denetimi_vekilden_once_calisir()
    {
        var program = Program();

        var guard = program.IndexOf("Sec-Fetch-Site", StringComparison.Ordinal);
        var proxy = program.IndexOf("app.MapReverseProxy();", StringComparison.Ordinal);

        guard.Should().BeGreaterThanOrEqualTo(0, "köken denetimi bulunamadı");
        proxy.Should().BeGreaterThanOrEqualTo(0, "vekil eşlemesi bulunamadı");

        guard.Should().BeLessThan(proxy,
            "denetim vekilden sonra kayıtlanırsa istek API'ye çoktan gitmiş olur");
    }

    [Fact]
    public void Vekil_tarayicinin_gonderdigi_jetonu_gecirmez()
    {
        var program = Program();

        program.Should().Contain("ctx.ProxyRequest.Headers.Authorization = null;",
            "oturum yokken transform erken dönüyor; temizlenmezse tarayıcının koyduğu Authorization " +
            "başlığı API'ye olduğu gibi iletilir");
    }
}
