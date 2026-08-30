using System.Text;
using System.Text.RegularExpressions;
using FluentAssertions;

namespace FurkanTural_DesignSystem.Tests;

/// <summary>Ön yüz kaynakları geçerli UTF-8 olmalı ve görünmez kontrol karakteri taşımamalı. CSS <c>content</c> değerleri betikle yazıldığında kaçış dizileri sessizce bozulabiliyor: Perl <c>"\2014"</c> dizisini sekizlik okuyup em dash yerine U+0081 + '4' üretmişti; madde işareti ve onay tiki sayfada bozuk glif olarak çiziliyordu ve hiçbir test görmüyordu.</summary>
public class SourceEncodingTests
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

    private static IEnumerable<string> FrontEndFiles()
    {
        var root = FindSolutionRoot();
        var ayrac = Path.DirectorySeparatorChar;

        foreach (var proje in new[] { "FurkanTural_Admin", "FurkanTural_Chat", "FurkanTural_Blog", "FurkanTural_Portfolio" })
        {
            var dizin = Path.Combine(root, "Presentation", proje);
            if (!Directory.Exists(dizin)) continue;

            foreach (var file in Directory.EnumerateFiles(dizin, "*.*", SearchOption.AllDirectories))
            {
                if (file.Contains($"{ayrac}bin{ayrac}") || file.Contains($"{ayrac}obj{ayrac}"))
                    continue;

                if (Path.GetExtension(file) is ".css" or ".js" or ".cshtml" or ".html")
                    yield return file;
            }
        }
    }

    [Fact]
    public void On_yuz_kaynaklari_gecerli_utf8()
    {
        var katiUtf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
        var sapan = new List<string>();
        var olculen = 0;

        foreach (var file in FrontEndFiles())
        {
            olculen++;

            try
            {
                katiUtf8.GetString(File.ReadAllBytes(file));
            }
            catch (DecoderFallbackException ex)
            {
                sapan.Add($"{Path.GetFileName(file)} → {ex.Message}");
            }
        }

        sapan.Should().BeEmpty(
            "geçersiz bayt tarayıcıda bozuk glif olarak çizilir; kaçış dizisi betikle yanlış yazıldığında böyle oluşur");

        olculen.Should().BeGreaterThan(20, "dosya taraması boşsa bu test hiçbir şey doğrulamıyor");
    }

    [Fact]
    public void Css_content_degerleri_kontrol_karakteri_tasimaz()
    {
        var sapan = new List<string>();
        var olculen = 0;

        foreach (var file in FrontEndFiles().Where(f => Path.GetExtension(f) == ".css"))
        {
            var css = File.ReadAllText(file);

            foreach (Match m in Regex.Matches(css, @"content:\s*""([^""]*)"""))
            {
                olculen++;

                var bozuk = m.Groups[1].Value.Where(char.IsControl).ToList();

                if (bozuk.Count > 0)
                {
                    var satir = css[..m.Index].Count(c => c == '\n') + 1;
                    var kodlar = string.Join(" ", bozuk.Select(c => $"U+{(int)c:X4}"));
                    sapan.Add($"{Path.GetFileName(file)}:{satir} → content içinde {kodlar}");
                }
            }
        }

        sapan.Should().BeEmpty(
            "Perl kaçış dizisini sekizlik okuyup görünmez bir karakter üretmişti; " +
            "madde işareti ve onay tiki sayfada bozuk çiziliyordu");

        olculen.Should().BeGreaterThan(3, "hiç content kuralı bulunamadıysa test bir şey doğrulamıyor");
    }
}
