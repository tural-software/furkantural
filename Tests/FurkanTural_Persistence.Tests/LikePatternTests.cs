using System.Text.RegularExpressions;
using FluentAssertions;
using FurkanTural_Persistence.Repositories.Concrete;

namespace FurkanTural_Persistence.Tests;

public class LikePatternTests
{
    private const string SolutionMarker = "FurkanTural.slnx";

    [Theory]
    [InlineData("merhaba", "%merhaba%")]
    [InlineData("100%", "%100\\%%")]
    [InlineData("ad_soyad", "%ad\\_soyad%")]
    [InlineData("[abc]", "%\\[abc]%")]
    [InlineData("c:\\yol", "%c:\\\\yol%")]
    public void Joker_karakterler_kacislanir(string value, string expected)
        => LikePattern.Contains(value).Should().Be(expected);

    [Fact]
    public void Yalnizca_jokerlerden_olusan_arama_desen_olarak_calismaz()
        => LikePattern.Contains("%_%").Should().Be("%\\%\\_\\%%",
            "kaçışlanmasaydı bu arama her satırla eşleşir ve tabloyu sıralı taramaya zorlardı");

    [Fact]
    public void Ham_sqldeki_her_LIKE_kacis_karakterini_bildirir()
    {
        var folder = Path.Combine(FindSolutionRoot(), "Infrastructure", "FurkanTural_Persistence");
        var separator = Path.DirectorySeparatorChar;

        var eksik = Directory.EnumerateFiles(folder, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{separator}obj{separator}")
                        && !p.Contains($"{separator}bin{separator}")
                        && !p.Contains($"{separator}Migrations{separator}"))
            .SelectMany(p => File.ReadAllLines(p).Select((line, i) => (File: Path.GetFileName(p), Line: i + 1, Text: line)))
            .Where(x => Regex.IsMatch(x.Text, @"\bLIKE\s+@\w+") && !x.Text.Contains("LikePattern.EscapeClause"))
            .Select(x => $"{x.File}:{x.Line}")
            .ToList();

        eksik.Should().BeEmpty(
            "kaçışlanan desen ESCAPE bildirilmeden gönderilirse SQL Server ters eğik çizgiyi sıradan karakter sayar ve arama bozulur:"
            + Environment.NewLine + string.Join(Environment.NewLine, eksik));
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
