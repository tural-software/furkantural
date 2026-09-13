using System.Reflection;
using FluentAssertions;
using FurkanTural_Blog.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_Blog.Tests;

public class LegalPagesTests
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

    private static string BlogFile(params string[] parts) =>
        File.ReadAllText(Path.Combine([FindSolutionRoot(), "Presentation", "FurkanTural_Blog", .. parts]));

    [Theory]
    [InlineData(nameof(HomeController.CommentRules), "yorum-kurallari", "BlogCommentRules")]
    [InlineData(nameof(HomeController.Imprint), "kunye", "BlogImprint")]
    public void Yasal_sayfalar_sabit_adreste_durur(string action, string template, string name)
    {
        var route = typeof(HomeController).GetMethod(action)!.GetCustomAttribute<RouteAttribute>();

        route.Should().NotBeNull($"{action} kendi Türkçe adresinde sunulmalı");
        route!.Template.Should().Be(template, "bu adres alt bilgide, yorum formunda ve sitemap'te geçiyor");
        route.Name.Should().Be(name);
    }

    [Theory]
    [InlineData("BlogCommentRules")]
    [InlineData("BlogImprint")]
    [InlineData("asp-action=\"Privacy\"")]
    public void Alt_bilgi_yasal_sayfalara_baglanir(string hedef)
    {
        BlogFile("Views", "Shared", "_Layout.cshtml").Should().Contain(hedef,
            "yasal metinler her sayfanın altından ulaşılabilir olmalı");
    }

    [Fact]
    public void Yorum_formu_kurallara_ve_gizlilik_politikasina_baglanir()
    {
        var form = BlogFile("Views", "Home", "_Comments.cshtml");

        form.Should().Contain("BlogCommentRules", "yorum yazan kişi hangi yorumun yayımlanmadığını göndermeden görebilmeli");
        form.Should().Contain("asp-action=\"Privacy\"", "ad ve e-posta toplanan formun yanında aydınlatma bağlantısı durmalı");
    }

    [Fact]
    public void Bulten_formu_gizlilik_politikasina_baglanir()
    {
        BlogFile("Views", "Newsletter", "Index.cshtml").Should().Contain("asp-action=\"Privacy\"",
            "e-posta toplanan formun yanında aydınlatma bağlantısı durmalı");
    }

    [Theory]
    [InlineData("/Home/Privacy")]
    [InlineData("/yorum-kurallari")]
    [InlineData("/kunye")]
    public void Sitemap_yasal_sayfalari_listeler(string path)
    {
        BlogFile("Controllers", "SeoController.cs").Should().Contain($"{{baseUrl}}{path}\"");
    }
}
