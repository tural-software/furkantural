using System.Text.RegularExpressions;
using FluentAssertions;

namespace FurkanTural_DesignSystem.Tests;

public class SharedTokenParityTests
{
    private const string SolutionMarker = "FurkanTural.slnx";

    private static readonly string[] SharedTokens =
    [
        "--font-sans",
        "--radius-sm",
        "--fs-3xs",
        "--fs-2xs",
        "--fs-xs",
        "--fs-sm",
        "--fs-md",
        "--fs-base",
        "--fs-lg",
        "--fs-xl",
        "--fs-2xl",
        "--fs-3xl",
        "--fs-title",
        "--fs-hero",
        "--fs-page",
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

    private static Dictionary<string, string> ReadTokens(string stylesheetPath)
    {
        var css = File.ReadAllText(stylesheetPath);
        var matches = Regex.Matches(css, @"^\s*(--[a-z0-9-]+)\s*:\s*([^;]+);", RegexOptions.Multiline);

        var tokens = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (Match match in matches)
        {
            var name = match.Groups[1].Value;
            var value = Regex.Replace(match.Groups[2].Value.Trim(), @"\s+", " ");

            if (!tokens.ContainsKey(name))
                tokens[name] = value;
        }

        return tokens;
    }

    private static Dictionary<string, Dictionary<string, string>> ReadAllProjects()
    {
        var root = FindSolutionRoot();

        return Stylesheets.ToDictionary(
            sheet => sheet.Project,
            sheet => ReadTokens(Path.Combine(root, sheet.RelativePath)),
            StringComparer.Ordinal);
    }

    [Fact]
    public void EveryStylesheet_ShouldExist()
    {
        var root = FindSolutionRoot();

        var missing = Stylesheets
            .Where(sheet => !File.Exists(Path.Combine(root, sheet.RelativePath)))
            .Select(sheet => $"{sheet.Project} → {sheet.RelativePath}")
            .ToList();

        missing.Should().BeEmpty(
            "ortak çatı testi dört sunum projesinin stil dosyasını okur; eksik olan(lar):"
            + Environment.NewLine + string.Join(Environment.NewLine, missing));
    }

    [Fact]
    public void EveryProject_ShouldDefine_AllSharedTokens()
    {
        var projects = ReadAllProjects();

        var missing = (
            from project in projects
            from token in SharedTokens
            where !project.Value.ContainsKey(token)
            select $"{project.Key}: {token}").ToList();

        missing.Should().BeEmpty(
            "ortak çatının tamamı dört projede de tanımlı olmalıdır; eksik olan(lar):"
            + Environment.NewLine + string.Join(Environment.NewLine, missing));
    }

    [Fact]
    public void SharedTokens_ShouldHaveIdenticalValues_AcrossProjects()
    {
        var projects = ReadAllProjects();
        var divergences = new List<string>();

        foreach (var token in SharedTokens)
        {
            var byValue = projects
                .Where(project => project.Value.ContainsKey(token))
                .GroupBy(project => project.Value[token], StringComparer.Ordinal)
                .ToList();

            if (byValue.Count <= 1)
                continue;

            var detail = string.Join(
                " | ",
                byValue.Select(group => $"[{string.Join(", ", group.Select(p => p.Key))}] = {group.Key}"));

            divergences.Add($"{token} → {detail}");
        }

        divergences.Should().BeEmpty(
            "ortak çatıdaki bir token dört projede farklı değer taşıyorsa çatı işlevini yitirir; sapan token(lar):"
            + Environment.NewLine + string.Join(Environment.NewLine, divergences));
    }
}
