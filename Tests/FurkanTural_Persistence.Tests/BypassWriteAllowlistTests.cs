using System.Text.RegularExpressions;
using FluentAssertions;

namespace FurkanTural_Persistence.Tests;

public class BypassWriteAllowlistTests
{
    private const string SolutionMarker = "FurkanTural.slnx";

    private static readonly HashSet<string> AllowedMethods =
    [
        "IncrementViewCountAsync",
        "TouchLastSeenAsync",
        "MarkConversationReadAsync",
        "TryConsumeTokenAsync",
        "ConsumePendingSubscriberVerificationsAsync",
        "PurgeAsync"
    ];

    private static readonly Regex BypassWrite = new(
        @"\bExecute(Update|Delete)(Async)?\b|\bExecuteSql(Raw|Interpolated)?(Async)?\b|\.Execute(Async)?\(",
        RegexOptions.Compiled);

    private static readonly Regex MethodDeclaration = new(
        @"(public|private|protected|internal)[^;{}=()]*?\s(?<ad>\w+)\s*(<[^>]*>)?\s*\(",
        RegexOptions.Compiled);

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

    private static IEnumerable<(string File, string Method)> BypassWrites()
    {
        var root = FindSolutionRoot();
        var folders = new[]
        {
            Path.Combine(root, "Infrastructure", "FurkanTural_Persistence"),
            Path.Combine(root, "Business", "FurkanTural_Business"),
            Path.Combine(root, "Web", "FurkanTural_API")
        };

        foreach (var folder in folders)
        {
            foreach (var path in Directory.EnumerateFiles(folder, "*.cs", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(root, path);
                if (relative.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                    || relative.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                    || relative.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}"))
                    continue;

                var source = File.ReadAllText(path);

                foreach (Match write in BypassWrite.Matches(source))
                {
                    var owner = MethodDeclaration.Matches(source[..write.Index]).LastOrDefault();
                    yield return (relative, owner?.Groups["ad"].Value ?? "?");
                }
            }
        }
    }

    [Fact]
    public void Tarama_bilinen_hedefli_yazmalari_bulur()
    {
        BypassWrites().Select(w => w.Method).Should().Contain(AllowedMethods,
            "bilinen üç hedefli yazma bulunamıyorsa tarama hiçbir şeyi doğrulamıyor demektir");
    }

    [Fact]
    public void Kaydetme_yolunu_atlayan_yazmalar_yalnizca_bilinen_metotlardadir()
    {
        var sapan = BypassWrites()
            .Where(w => !AllowedMethods.Contains(w.Method))
            .Select(w => $"{w.File} → {w.Method}")
            .Distinct()
            .ToList();

        sapan.Should().BeEmpty(
            "kaydetme yolunu atlayan yazma canlı bildirim kancasına görünmez; izlenen bir tabloya bu yoldan yazılırsa açık duran panel değişikliği hiç duymaz. " +
            "Yeni bir hedefli yazma bilinçli bir karardır ve listeye adıyla girer:" + Environment.NewLine + string.Join(Environment.NewLine, sapan));
    }
}
