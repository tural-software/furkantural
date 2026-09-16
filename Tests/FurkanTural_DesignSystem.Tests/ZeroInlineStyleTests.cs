using System.Text.RegularExpressions;
using FluentAssertions;

namespace FurkanTural_DesignSystem.Tests;

/// <summary>Dört sitenin içerik güvenliği kuralında satır içi stile de izin yoktur. Kural kalktığı anda her <c>style="..."</c> özniteliği sessizce etkisiz kalır: tarayıcı hata göstermez, yalnızca konsola yazar ve sayfa bozuk görünür. Dinamik değerler (renk, yüzde, avatar) <c>data-</c> özniteliğiyle taşınır ve <c>dynamic-style.js</c> onları doğrulayıp CSSOM ile uygular; CSSOM kuraldan etkilenmez.<para>Tek istisna panelin e-posta önizleme belgesidir: e-posta HTML'i satır içi stille yazılır ve o belge betiksiz, sandbox'lı kendi kuralıyla ayrı bir uçtan sunulur.</para></summary>
public class ZeroInlineStyleTests
{
    private const string SolutionMarker = "FurkanTural.slnx";

    public static TheoryData<string> Projects => new() { "FurkanTural_Admin", "FurkanTural_Chat", "FurkanTural_Blog", "FurkanTural_Portfolio" };

    private static string Root()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, SolutionMarker)))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException(SolutionMarker);
    }

    private static bool Skipped(string path)
    {
        var sep = Path.DirectorySeparatorChar;
        return path.Contains($"{sep}lib{sep}") || path.Contains($"{sep}bin{sep}") || path.Contains($"{sep}obj{sep}")
            || path.EndsWith(".min.js", StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(Projects))]
    public void Stil_kurali_satir_ici_stile_izin_vermez(string project)
    {
        var program = File.ReadAllText(Path.Combine(Root(), "Presentation", project, "Program.cs"));
        var styleSrc = Regex.Match(program, "style-src[^;]*;").Value;

        styleSrc.Should().NotBeEmpty();
        styleSrc.Should().NotContain("'unsafe-inline'");
        styleSrc.Should().Contain("'nonce-{nonce}'");
    }

    [Theory]
    [MemberData(nameof(Projects))]
    public void Gorunumlerde_stil_ozniteligi_yoktur(string project)
    {
        var bulunan = Directory.EnumerateFiles(Path.Combine(Root(), "Presentation", project, "Views"), "*.cshtml", SearchOption.AllDirectories)
            .Where(p => Regex.IsMatch(File.ReadAllText(p), "\\sstyle\\s*=\\s*\"", RegexOptions.IgnoreCase))
            .Select(p => Path.GetRelativePath(Root(), p))
            .ToList();

        bulunan.Should().BeEmpty("kural satır içi stili engeller; değer sınıfa ya da data- özniteliğine taşınmalı:"
            + Environment.NewLine + string.Join(Environment.NewLine, bulunan));
    }

    [Theory]
    [MemberData(nameof(Projects))]
    public void Betiklerin_urettigi_HTMLde_stil_ozniteligi_yoktur(string project)
    {
        var folder = Path.Combine(Root(), "Presentation", project, "wwwroot", "js");
        if (!Directory.Exists(folder))
            return;

        var bulunan = Directory.EnumerateFiles(folder, "*.js", SearchOption.AllDirectories)
            .Where(p => !Skipped(p))
            .Where(p => Regex.IsMatch(File.ReadAllText(p), "style=\\\\?[\"']|setAttribute\\(\\s*['\"]style['\"]"))
            .Select(p => Path.GetRelativePath(Root(), p))
            .ToList();

        bulunan.Should().BeEmpty("innerHTML ile eklenen style özniteliği de kural tarafından engellenir; CSSOM ya da sınıf kullanılmalı:"
            + Environment.NewLine + string.Join(Environment.NewLine, bulunan));
    }

    [Fact]
    public void Satir_ici_stile_izin_veren_tek_yanit_onizleme_belgesidir()
    {
        var root = Root();
        var bulunan = new[] { "Presentation", "Web" }
            .SelectMany(folder => Directory.EnumerateFiles(Path.Combine(root, folder), "*.cs", SearchOption.AllDirectories))
            .Where(p => !Skipped(p))
            .Where(p => File.ReadAllLines(p).Any(line => !line.TrimStart().StartsWith("//", StringComparison.Ordinal)
                                                         && line.Contains("'unsafe-inline'", StringComparison.Ordinal)))
            .Select(p => Path.GetRelativePath(root, p))
            .ToList();

        bulunan.Should().BeEquivalentTo([Path.Combine("Presentation", "FurkanTural_Admin", "Controllers", "PreviewController.cs")]);

        var preview = File.ReadAllText(Path.Combine(root, "Presentation", "FurkanTural_Admin", "Controllers", "PreviewController.cs"));
        preview.Should().Contain("default-src 'none'", "önizleme belgesinde betik hiçbir kaynaktan çalışmamalı");
        preview.Should().Contain("sandbox", "belge panelin kökenine erişememeli");
        preview.Should().NotContain("script-src");
    }

    [Fact]
    public void Onizleme_panele_gomulu_degil_ayri_belgeden_yuklenir()
    {
        var modal = File.ReadAllText(Path.Combine(Root(), "Presentation", "FurkanTural_Admin", "wwwroot", "js", "html-preview-modal.js"));

        modal.Should().NotContain("srcdoc", "srcdoc belgesi panelin kuralını miras alır ve e-posta stilsiz görünür");
        modal.Should().Contain("sandbox=\"\"");
    }

    [Theory]
    [InlineData("FurkanTural_Admin")]
    [InlineData("FurkanTural_Chat")]
    [InlineData("FurkanTural_Blog")]
    public void Dinamik_stil_betigi_yerlesimde_yuklenir(string project)
    {
        File.ReadAllText(Path.Combine(Root(), "Presentation", project, "Views", "Shared", "_Layout.cshtml"))
            .Should().Contain("<script src=\"~/js/dynamic-style.js\" asp-append-version=\"true\"></script>");
    }
}
