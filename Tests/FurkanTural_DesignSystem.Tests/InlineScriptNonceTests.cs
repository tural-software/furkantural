using System.Text.RegularExpressions;
using FluentAssertions;

namespace FurkanTural_DesignSystem.Tests;

/// <summary>Admin, Blog ve Portfolio'nun içerik güvenliği kuralında <c>'unsafe-inline'</c> yoktur; satır içi betik yalnızca istek başına üretilen nonce ile çalışır. Kural bir XSS açığında son savunma hattıdır: nonce taşımayan enjekte edilmiş betik çalışmaz.<para>Bunun bedeli, nonce'u unutulan her yeni satır içi betiğin ve her satır içi olay işleyicisinin sessizce çalışmamasıdır. Tarayıcı hata göstermez, yalnızca konsola yazar; bu testler o sessizliği derleme anına taşır.</para></summary>
public class InlineScriptNonceTests
{
    private const string SolutionMarker = "FurkanTural.slnx";

    public static TheoryData<string> Projects => new() { "FurkanTural_Admin", "FurkanTural_Blog", "FurkanTural_Portfolio", "FurkanTural_Chat" };

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

    private static IEnumerable<(string File, string Source)> Views(string project)
        => Directory.EnumerateFiles(Path.Combine(FindSolutionRoot(), "Presentation", project, "Views"), "*.cshtml", SearchOption.AllDirectories)
            .Select(p => (Path.GetRelativePath(FindSolutionRoot(), p),
                string.Join('\n', File.ReadAllLines(p).Where(line => !line.TrimStart().StartsWith("//", StringComparison.Ordinal)))));

    [Theory]
    [MemberData(nameof(Projects))]
    public void Betik_kurali_satir_ici_calismaya_izin_vermez(string project)
    {
        var program = File.ReadAllText(Path.Combine(FindSolutionRoot(), "Presentation", project, "Program.cs"));
        var scriptSrc = Regex.Match(program, "script-src[^;]*;").Value;

        scriptSrc.Should().NotBeEmpty();
        scriptSrc.Should().NotContain("'unsafe-inline'", "satır içi betiğe genel izin, enjekte edilmiş betiğe de izin demektir");
        scriptSrc.Should().Contain("'nonce-{nonce}'");
        program.Should().Contain("context.Items[\"csp-nonce\"] = nonce;", "görünümler nonce'u buradan okur");
    }

    [Theory]
    [MemberData(nameof(Projects))]
    public void Calisan_her_satir_ici_betik_nonce_tasir(string project)
    {
        var eksik = Views(project)
            .SelectMany(v => Regex.Matches(v.Source, "<script(?<attrs>[^>]*)>")
                .Where(m => !m.Groups["attrs"].Value.Contains("src=")
                            && !m.Groups["attrs"].Value.Contains("application/json")
                            && !m.Groups["attrs"].Value.Contains("application/ld+json")
                            && !m.Groups["attrs"].Value.Contains("nonce="))
                .Select(_ => v.File))
            .Distinct()
            .ToList();

        eksik.Should().BeEmpty("nonce taşımayan satır içi betik kural tarafından sessizce engellenir:"
            + Environment.NewLine + string.Join(Environment.NewLine, eksik));
    }

    [Theory]
    [MemberData(nameof(Projects))]
    public void Gorunumlerde_satir_ici_olay_isleyicisi_yoktur(string project)
    {
        var bulunan = Views(project)
            .Where(v => Regex.IsMatch(v.Source, "\\son(click|change|submit|input|load|error|keyup|keydown|focus|blur)\\s*=\\s*\"", RegexOptions.IgnoreCase))
            .Select(v => v.File)
            .ToList();

        bulunan.Should().BeEmpty("olay işleyicisi özniteliği nonce taşıyamaz ve kural onu hiç çalıştırmaz; işleyici betik dosyasında bağlanmalı:"
            + Environment.NewLine + string.Join(Environment.NewLine, bulunan));
    }
}
