using System.Text.RegularExpressions;
using FluentAssertions;

namespace FurkanTural_DesignSystem.Tests;

/// <summary>Dört projenin renk sözlükleri farklı (Admin/Chat <c>--bg-main</c>, Blog <c>--bg</c>, Portfolio <c>--color-bg</c>). Ortak <b>rol adları</b> her dosyada tanımlıdır; Blog ve Portfolio bunları kendi tokenlarına takma ad olarak bağlar. Bu testler o ortak çatının delinmediğini doğrular — değerler değil, rollerin varlığı denetlenir.</summary>
public class SharedColorRoleTests
{
    private const string SolutionMarker = "FurkanTural.slnx";

    private static readonly string[] ColorRoles =
    [
        "--bg-main",
        "--bg-card",
        "--bg-card-solid",
        "--text-main",
        "--text-dim",
        "--border-color",
        "--divider",
        "--accent",
        "--accent-rgb",
        "--accent-soft",
    ];

    private static readonly (string Project, string RelativePath)[] Stylesheets =
    [
        ("Admin",     @"Presentation\FurkanTural_Admin\wwwroot\css\theme.css"),
        ("Chat",      @"Presentation\FurkanTural_Chat\wwwroot\css\theme.css"),
        ("Blog",      @"Presentation\FurkanTural_Blog\wwwroot\css\site.css"),
        ("Portfolio", @"Presentation\FurkanTural_Portfolio\wwwroot\css\site.css"),
    ];

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

    private static HashSet<string> DeclaredTokens(string path)
    {
        var css = File.ReadAllText(path);
        var matches = Regex.Matches(css, @"^\s*(--[a-z0-9-]+)\s*:", RegexOptions.Multiline);

        return [.. matches.Select(m => m.Groups[1].Value)];
    }

    [Fact]
    public void Dort_projede_de_ortak_renk_rolleri_tanimlidir()
    {
        var root = FindSolutionRoot();
        var eksik = new List<string>();

        foreach (var (project, relative) in Stylesheets)
        {
            var declared = DeclaredTokens(Path.Combine(root, relative));

            foreach (var role in ColorRoles)
            {
                if (!declared.Contains(role))
                    eksik.Add($"{project} → {role}");
            }
        }

        eksik.Should().BeEmpty("ortak bir bileşen CSS'i bu adlarla yazıldığında dört projede de çalışmalı");
    }

    [Fact]
    public void Admin_panelinde_sabit_renk_kalmadi()
    {
        var root = FindSolutionRoot();
        var cssRoot = Path.Combine(root, "Presentation", "FurkanTural_Admin", "wwwroot", "css");

        // theme.css tokenların TANIMLANDIĞI yer; oradaki değerler doğal olarak sabittir.
        // html-preview-modal iframe'i bilerek beyaz: e-posta şablonu beyaz zeminde çizilir.
        var muaf = new[] { "theme.css", "html-preview-modal.css" };

        var ihlal = new List<string>();

        foreach (var path in Directory.EnumerateFiles(cssRoot, "*.css", SearchOption.AllDirectories))
        {
            if (muaf.Contains(Path.GetFileName(path))) continue;

            var css = File.ReadAllText(path);
            foreach (Match m in Regex.Matches(css, @"#[0-9a-fA-F]{3,8}\b"))
                ihlal.Add($"{Path.GetFileName(path)} → {m.Value}");
        }

        ihlal.Should().BeEmpty(
            "koyu temaya sabitlenmiş renk açık temada okunmaz hale gelir; rol tokenları kullanılmalı");
    }
}
