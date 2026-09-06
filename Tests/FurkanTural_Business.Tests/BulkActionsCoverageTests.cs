using System.Reflection;
using FluentAssertions;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Business.Services.Concrete;

namespace FurkanTural_Business.Tests;

/// <summary>Toplu işlem tek gövdeden çalışır: her servis kendi deposunu verip ortak yardımcıya devreder. Bu test yeni bir servis eklendiğinde ya da biri kendi kopyasını yazmaya kalktığında kırılır — kural kodda değil burada yazılıdır.</summary>
public class BulkActionsCoverageTests
{
    private static readonly Type[] Services = typeof(BlogService).Assembly
        .GetTypes()
        .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IBulkService).IsAssignableFrom(t))
        .ToArray();

    [Fact]
    public void Yirmi_modul_toplu_islem_sozlesmesini_tasir()
    {
        Services.Select(t => t.Name).Should().HaveCount(20,
            "panelde satır eylemi olan yirmi modülün hepsi toplu işlem sunar; kayıt defteri tek istisnadır çünkü satırı silinmez");
        Services.Select(t => t.Name).Should().Contain(["BlogService", "UserService", "ReportService", "CallLogService"]);
        Services.Select(t => t.Name).Should().NotContain("LogService");
    }

    [Fact]
    public void Her_servis_ortak_govdeye_devreder_kendi_kopyasini_yazmaz()
    {
        var own = Services
            .Select(t => t.GetMethod(nameof(IBulkService.BulkAsync), BindingFlags.Public | BindingFlags.Instance))
            .Where(m => m is not null)
            .Select(m => m!)
            .Where(m => m.GetMethodBody()?.GetILAsByteArray()?.Length > 120)
            .Select(m => m.DeclaringType!.Name)
            .ToArray();

        own.Should().BeEmpty(
            "gövdesi büyüyen servis ortak yardımcıya devretmiyor demektir; kural yirmi yerde ayrı ayrı yazılırsa biri eskir:" +
            Environment.NewLine + string.Join(Environment.NewLine, own.Select(n => "  - " + n)));
    }
}
