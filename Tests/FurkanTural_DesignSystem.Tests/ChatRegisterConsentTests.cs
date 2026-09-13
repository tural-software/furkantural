using System.Text.RegularExpressions;
using FluentAssertions;

namespace FurkanTural_DesignSystem.Tests;

public class ChatRegisterConsentTests
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

    private static string RegisterView() =>
        File.ReadAllText(Path.Combine(FindSolutionRoot(), "Presentation", "FurkanTural_Chat", "Views", "Account", "Register.cshtml"));

    private static IReadOnlyList<string> CheckLabels(string view) =>
        Regex.Matches(view, @"<label class=""agreement-check"">(.*?)</label>", RegexOptions.Singleline)
            .Select(m => m.Groups[1].Value)
            .ToList();

    [Fact]
    public void Sozlesme_onayi_ve_yas_beyani_ayri_kutulardir()
    {
        var labels = CheckLabels(RegisterView());

        labels.Should().HaveCount(2, "sözleşmeyi kabul etmek ile 18 yaşını doldurduğunu beyan etmek ayrı iradelerdir");
        labels.Should().ContainSingle(l => l.Contains("asp-for=\"AcceptAgreement\""));
        labels.Should().ContainSingle(l => l.Contains("asp-for=\"ConfirmAdult\"") && l.Contains("18 yaşını doldurdum"));
    }

    [Fact]
    public void Aydinlatma_metni_onaylanan_bir_kutunun_icinde_durmaz()
    {
        var view = RegisterView();

        CheckLabels(view).Should().NotContain(l => l.Contains("asp-action=\"Privacy\""),
            "aydınlatma metni onaylanmaz, yalnızca okunur; kabul kutusunun içine konursa açık rıza gibi görünür");
        view.Should().Contain("asp-action=\"Privacy\"", "kayıt ekranı aydınlatma metnine bağlantı vermeli");
    }

    [Fact]
    public void Sozlesme_kutusu_topluluk_kurallarina_baglanir()
    {
        CheckLabels(RegisterView()).Should().ContainSingle(l =>
                l.Contains("asp-action=\"Agreement\"") && l.Contains("asp-action=\"Rules\""),
            "topluluk kuralları sözleşmenin parçası; kabul edilen metin ikisini birlikte göstermeli");
    }
}
