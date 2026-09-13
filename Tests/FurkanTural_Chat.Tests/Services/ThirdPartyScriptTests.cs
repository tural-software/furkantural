using FluentAssertions;

namespace FurkanTural_Chat.Tests.Services;

public class ThirdPartyScriptTests
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

    private static string ChatRoot() => Path.Combine(FindSolutionRoot(), "Presentation", "FurkanTural_Chat");

    private static bool IsSkipped(string path)
    {
        var sep = Path.DirectorySeparatorChar;
        return path.Contains($"{sep}lib{sep}") || path.Contains($"{sep}bin{sep}") || path.Contains($"{sep}obj{sep}");
    }

    [Fact]
    public void Sohbet_kitapligi_yerel_kopyadan_ve_sohbet_betiginden_once_yuklenir()
    {
        var root = ChatRoot();
        var view = File.ReadAllText(Path.Combine(root, "Views", "Chat", "Index.cshtml"));

        var library = view.IndexOf("~/lib/signalr/signalr.min.js", StringComparison.Ordinal);
        var client = view.IndexOf("~/js/chat.js", StringComparison.Ordinal);

        library.Should().BeGreaterThan(-1,
            "SignalR istemcisi kendi sunucumuzdan gelmeli; CDN her ziyaretçinin IP adresini yurt dışındaki bir sağlayıcıya taşır");
        client.Should().BeGreaterThan(library,
            "sohbet betiği kitaplıktan önce çalışırsa bağlantıyı hiç kuramaz");
        File.Exists(Path.Combine(root, "wwwroot", "lib", "signalr", "signalr.min.js")).Should().BeTrue(
            "görünüm yerel kopyayı işaret ediyor; dosya yoksa sohbet ekranı açılmaz");
    }

    [Fact]
    public void Site_hicbir_kaynagi_jsdelivrden_istemez()
    {
        var root = ChatRoot();
        var files = Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".cs") || f.EndsWith(".cshtml") || f.EndsWith(".js"))
            .Where(f => !IsSkipped(f))
            .ToList();

        files.Count.Should().BeGreaterThan(20, "tarama boş dönerse bu test hiçbir şey doğrulamıyor");

        files.Where(f => File.ReadAllText(f).Contains("cdn.jsdelivr.net", StringComparison.OrdinalIgnoreCase))
            .Select(f => Path.GetRelativePath(root, f))
            .Should().BeEmpty(
                "içerik güvenliği kuralı bu kaynağa izin vermiyor; oradan istenen betik sessizce engellenir " +
                "ve istek ziyaretçinin IP adresini yurt dışındaki bir sağlayıcıya taşır");
    }
}
