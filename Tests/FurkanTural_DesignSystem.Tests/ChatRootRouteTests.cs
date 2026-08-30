using System.Text.RegularExpressions;
using FluentAssertions;

namespace FurkanTural_DesignSystem.Tests;

/// <summary>Kök adres açılış sayfasını sunar; giriş formu kapıda durmaz. Oturumu açık olan ziyaretçi tanıtım sayfasını değil sohbetini görür — aksi hâlde her açılışta pazarlama metnini geçmesi gerekir.</summary>
public class ChatRootRouteTests
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

    [Fact]
    public void Kok_adres_acilis_sayfasini_sunar()
    {
        var program = ChatFile("Program.cs");

        var rota = Regex.Match(program,
            @"name:\s*""root"",\s*pattern:\s*"""",\s*defaults:\s*new\s*\{([^}]*)\}",
            RegexOptions.Singleline);

        rota.Success.Should().BeTrue("kök rota tanımı bulunamadı");

        rota.Groups[1].Value.Should().Contain("controller = \"Home\"");
        rota.Groups[1].Value.Should().Contain("action = \"Index\"",
            "açılış sayfası yalnızca /Home/Index adresinde durursa kimse görmez; " +
            "handoff'ta markanın ve '‹ Ana sayfa' bağlantısının hedefi burasıdır");
    }

    [Fact]
    public void Oturumu_acik_ziyaretci_acilista_tutulmaz()
    {
        var controller = ChatFile("Controllers", "HomeController.cs");

        var index = Regex.Match(controller,
            @"public IActionResult Index\(\)\s*\{(.*?)\n    \}",
            RegexOptions.Singleline);

        index.Success.Should().BeTrue("Home.Index bulunamadı");

        index.Groups[1].Value.Should().Contain("Session.GetString(\"token\")",
            "kök adres artık açılışı sunuyor; oturum denetimi olmadan giriş yapmış kullanıcı " +
            "her seferinde tanıtım sayfasını geçmek zorunda kalır");
        index.Groups[1].Value.Should().Contain("RedirectToAction(\"Index\", \"Chat\")");
    }

    [Fact]
    public void Acilis_sayfasi_dizine_eklenebilir_kalir()
    {
        ChatFile("Views", "Home", "Index.cshtml").Should().Contain("\"index, follow\"",
            "düzenin varsayılanı noindex; sitenin kök adresi arama motoruna kapalı kalmamalı");
    }

    [Fact]
    public void Kok_rota_ile_varsayilan_rota_ayni_hedefte_anlasir()
    {
        var program = ChatFile("Program.cs");

        var kok = Regex.Match(program,
            @"name:\s*""root"",\s*pattern:\s*"""",\s*defaults:\s*new\s*\{\s*controller\s*=\s*""(?<controller>\w+)""\s*,\s*action\s*=\s*""(?<action>\w+)""",
            RegexOptions.Singleline);

        var varsayilan = Regex.Match(program,
            @"name:\s*""default"",\s*pattern:\s*""\{controller=(?<controller>\w+)\}/\{action=(?<action>\w+)\}",
            RegexOptions.Singleline);

        kok.Success.Should().BeTrue("kök rota tanımı bulunamadı");
        varsayilan.Success.Should().BeTrue("varsayılan rota tanımı bulunamadı");

        var gerekce =
            "ASP.NET, varsayılanlarıyla aynı olan sondaki segmentleri atar: varsayılan rota " +
            $"{varsayilan.Groups["controller"].Value}/{varsayilan.Groups["action"].Value} ise o eyleme üretilen " +
            "her bağlantı \"/\" olur. Kök rota başka bir yeri gösteriyorsa o bağlantı sessizce yanlış yere gider " +
            "ve bu bir form action'ı olduğunda gönderim yanlış uca düşer.";

        varsayilan.Groups["controller"].Value.Should().Be(kok.Groups["controller"].Value, gerekce);
        varsayilan.Groups["action"].Value.Should().Be(kok.Groups["action"].Value, gerekce);
    }
}
