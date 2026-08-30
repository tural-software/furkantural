using System.Text.RegularExpressions;
using FluentAssertions;

namespace FurkanTural_Chat.Tests.Services;

/// <summary>CSP kaynak ifadesi bozuksa tarayıcı o girdiyi <b>sessizce yok sayar</b> ve konsola hata basar: politika yürürlüktedir ama o satır yoktur. <c>ws:localhost:7000</c> tam olarak böyleydi — şema ayıracı düşmüştü, her sayfa açılışında hata veriyordu ve WebSocket izni yalnızca listedeki çıplak <c>ws:</c> sayesinde ayakta kalıyordu.</summary>
public class CspSourceFormatTests
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
    public void Websocket_semasi_ayracini_kaybetmez()
    {
        var program = Program();

        Regex.IsMatch(program, @"Replace\(""http://"",\s*""ws:""\s*\)").Should().BeFalse(
            "'ws:' + ana bilgisayar geçerli bir kaynak ifadesi değil; tarayıcı girdiyi yok sayıp " +
            "her sayfada konsola hata basıyordu");

        Regex.IsMatch(program, @"Replace\(""http://"",\s*""ws://""\s*\)").Should().BeTrue(
            "http tabanı ws:// şemasına çevrilmeli");

        Regex.IsMatch(program, @"Replace\(""https://"",\s*""wss://""\s*\)").Should().BeTrue(
            "https tabanı wss:// şemasına çevrilmeli");
    }

    [Fact]
    public void Sema_donusumu_dogru_sirada_uygulanir()
    {
        var program = Program();
        var https = program.IndexOf(@"Replace(""https://""", StringComparison.Ordinal);
        var http = program.IndexOf(@"Replace(""http://""", StringComparison.Ordinal);

        https.Should().BeGreaterThanOrEqualTo(0, "https dönüşümü bulunamadı");
        http.Should().BeGreaterThanOrEqualTo(0, "http dönüşümü bulunamadı");

        https.Should().BeLessThan(http,
            "'http://' önce uygulanırsa 'https://' tabanı da ona takılır ve 'wss://' hiç oluşmaz");
    }
}
