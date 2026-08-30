using System.Text.RegularExpressions;
using FluentAssertions;

namespace FurkanTural_DesignSystem.Tests;

/// <summary>Turnstile doğrulaması async yüklendiği için gizli alan sayfa açılışında boştur. Gönder düğmesi kilitli başlamaz ya da gönderim öncesi kontrol edilmezse kullanıcı doğrulama bitmeden gönderir ve suçu kendisine atan bir hata alır. Token tek kullanımlık ve süreli olduğundan hata sonrası temizlenmeli, aksi hâlde tekrar denemede aynı geçersiz token gider.</summary>
public class TurnstileGateContractTests
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

    private static string ChatFile(params string[] parts) =>
        File.ReadAllText(Path.Combine([FindSolutionRoot(), "Presentation", "FurkanTural_Chat", .. parts]));

    private static IEnumerable<(string Name, string Content)> AuthViews()
    {
        foreach (var name in new[] { "Login.cshtml", "Register.cshtml" })
            yield return (name, ChatFile("Views", "Account", name));
    }

    [Fact]
    public void Dogrulama_bitmeden_gonderilemez()
    {
        var sapan = new List<string>();

        foreach (var (name, content) in AuthViews())
        {
            var button = Regex.Match(content, @"<button type=""submit""[^>]*>", RegexOptions.Singleline);

            if (!button.Success)
                sapan.Add($"{name}: gönder düğmesi bulunamadı");
            else if (Regex.IsMatch(button.Value, @"disabled=""@\("))
                sapan.Add($"{name}: düğme yalnızca site anahtarı VARKEN kilitleniyor; " +
                          "anahtar alınamadığında kapı açık kalıyordu");
            else if (!Regex.IsMatch(button.Value, @"(^|\s)disabled(\s|>|=)"))
                sapan.Add($"{name}: düğme kilitli başlamıyor");
        }

        sapan.Should().BeEmpty(
            "Cloudflare betiği async yüklenir; düğme açık başlarsa kullanıcı token gelmeden gönderir. " +
            "Kapıyı yalnızca doğrulama açar: widget hiç çizilmediyse düğme kilitli kalmalı");
    }

    [Fact]
    public void Suresi_dolan_ve_hata_veren_dogrulama_karsilanir()
    {
        var sapan = new List<string>();

        foreach (var (name, content) in AuthViews())
        {
            foreach (var geri in new[] { "data-callback", "data-expired-callback", "data-error-callback" })
            {
                if (!content.Contains(geri))
                    sapan.Add($"{name}: {geri} yok");
            }
        }

        sapan.Should().BeEmpty(
            "token ~5 dakikada eskir; süre dolduğunda ya da doğrulama hata verdiğinde sayfanın haberi olmalı");
    }

    [Fact]
    public void Iki_sayfa_da_ortak_kapiyi_kullanir()
    {
        var sapan = new List<string>();

        foreach (var (name, content) in AuthViews())
        {
            if (!content.Contains("js/turnstile-gate.js"))
                sapan.Add($"{name}: ortak kapı betiğini yüklemiyor");

            if (content.Contains("function onTurnstileSuccess"))
                sapan.Add($"{name}: geri çağrının kendi kopyasını taşıyor");
        }

        sapan.Should().BeEmpty("mantık iki görünümde çoğaltılırsa biri güncellenip diğeri geride kalır");
    }

    [Fact]
    public void Gonderim_oncesi_token_kontrol_edilir_ve_basarisizlikta_temizlenir()
    {
        var auth = ChatFile("wwwroot", "js", "auth.js");

        auth.Should().Contain("tokenInput && !tokenInput.value",
            "boş token sunucuya gitmemeli; giden istek kullanıcıyı suçlayan bir hata döndürüyor");
        auth.Should().Contain("window.ftTurnstileReset",
            "token tek kullanımlıktır; başarısızlıktan sonra temizlenmezse tekrar denemede aynısı gider");
        auth.Should().NotContain("window.turnstile.reset()",
            "sıfırlama artık ortak kapıdan geçiyor — widget'ı sıfırlayıp gizli alanı bırakmak eski hatanın kendisiydi");
    }

    [Fact]
    public void Site_anahtari_yokken_kullaniciya_sebebi_soylenir()
    {
        foreach (var (name, content) in AuthViews())
        {
            var ipucu = Regex.Match(content, @"id=""turnstileHint""[^>]*>([^<]*)<");

            ipucu.Success.Should().BeTrue($"{name}: ipucu alanı bulunamadı");
            ipucu.Groups[1].Value.Should().Contain("TurnstileSiteKey",
                $"{name}: anahtar alınamadığında kutu boş kalıyordu; kullanıcı kilitli düğmenin sebebini göremiyordu");
        }
    }
}
