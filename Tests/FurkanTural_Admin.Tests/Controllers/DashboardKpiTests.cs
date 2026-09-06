using FluentAssertions;
using FurkanTural_Admin.Controllers;
using FurkanTural_Admin.Helpers;
using FurkanTural_Admin.Models.Common;
using FurkanTural_Admin.Models.Dashboard;
using FurkanTural_Admin.Services;
using FurkanTural_Admin.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Moq;

namespace FurkanTural_Admin.Tests.Controllers;

/// <summary>Panel tek toplayıcı yanıttan beslenir: dört gösterge, "Dikkat gerektiren" şeridi ve kart rozetleri aynı nesneden türetilir. "Bu hafta yeni" dört sayacı toplar ve önceki haftaya göre farkı ok olarak taşır; toplayıcı cevap vermezse her gösterge tire kalır, ne şerit ne rozet çizilir, uydurma sıfır yazılmaz.</summary>
public class DashboardKpiTests
{
    private static DashboardController BuildSut(AdminDashboardModel? data, out Mock<IAdminDashboardClient> client)
    {
        client = new Mock<IAdminDashboardClient>();
        client.Setup(c => c.GetAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(data);

        var url = new Mock<IUrlHelper>();
        url.Setup(x => x.Action(It.IsAny<UrlActionContext>())).Returns<UrlActionContext>(x => "/" + x.Controller);

        return new DashboardController(client.Object)
        {
            ControllerContext = ControllerTestHelper.BuildControllerContext("token"),
            Url = url.Object
        };
    }

    private static DashboardViewModel ViewModelOf(IActionResult result)
        => result.Should().BeOfType<ViewResult>().Which.Model.Should().BeOfType<DashboardViewModel>().Subject;

    private static AdminDashboardModel Sample() => new()
    {
        Summaries = new Dictionary<string, EntitySummaryModel>(StringComparer.OrdinalIgnoreCase)
        {
            ["blog"] = new() { TotalCount = 28, LastActivityAt = new DateTime(2026, 9, 1) },
            ["user"] = new() { TotalCount = 12 },
            ["log"] = new() { TotalCount = 260 }
        },
        UnreadContacts = 3,
        PendingReports = 2,
        ActiveUsers = 4,
        ThisWeek = new AdminWeeklyCountsModel { Blogs = 2, Users = 5, Contacts = 3, Subscribers = 1 },
        LastWeek = new AdminWeeklyCountsModel { Blogs = 1, Users = 2, Contacts = 0, Subscribers = 1 }
    };

    [Fact]
    public async Task Tek_istekle_dort_gosterge_serit_ve_rozetler_dolar()
    {
        var sut = BuildSut(Sample(), out var client);

        var vm = ViewModelOf(await sut.Index(CancellationToken.None));

        client.Verify(c => c.GetAsync(DashboardController.WindowDays, "token", It.IsAny<CancellationToken>()), Times.Once,
            "pano tek toplayıcı istekle dolar; otuz iki ayrı istek dönemi kapandı");
        vm.TotalRecordCount.Should().Be(300);
        vm.Kpis.Select(k => k.Key).Should().Equal(new[] { "records", "fresh", "active-users", "open-work" });

        var fresh = vm.Kpis.Single(k => k.Key == "fresh");
        fresh.Value.Should().Be("11");
        fresh.Detail.Should().Be("2 yazı · 5 kullanıcı · 3 mesaj · 1 abone");
        fresh.Trend.Should().Be(7, "bu hafta 11, geçen hafta 4");
        fresh.TrendText.Should().Be("geçen hafta 4");
        vm.Kpis.Single(k => k.Key == "active-users").Value.Should().Be("4");

        var open = vm.Kpis.Single(k => k.Key == "open-work");
        open.Value.Should().Be("5");
        open.Detail.Should().Be("3 okunmamış mesaj · 2 bekleyen şikayet");
        open.Url.Should().Be("#dash-attention-title");

        vm.Attention.Select(a => (a.Slug, a.Count)).Should().Equal(("contact", 3), ("reports", 2));
        vm.Attention.Select(a => a.Url).Should().Equal("/Contact", "/Report");

        var cards = vm.Groups.SelectMany(g => g.Modules).ToDictionary(c => c.Slug);
        cards["blogs"].TotalCount.Should().Be(28, "özet anahtarı modülün API yol adıyla eşleşir");
        cards["blogs"].LastActivityAt.Should().Be(new DateTime(2026, 9, 1));
        cards["categories"].TotalCount.Should().BeNull("toplayıcının vermediği varlık boş kalır");
        cards["contact"].AttentionCount.Should().Be(3);
        cards["reports"].AttentionCount.Should().Be(2);
        cards.Values.Where(c => c.Slug is not ("contact" or "reports")).Should().OnlyContain(c => c.AttentionCount == null);
    }

    [Fact]
    public async Task Toplayici_cevap_vermezse_tire_kalir_serit_ve_rozet_cizilmez()
    {
        var sut = BuildSut(null, out _);

        var vm = ViewModelOf(await sut.Index(CancellationToken.None));

        vm.TotalRecordCount.Should().BeNull();
        vm.Kpis.Select(k => k.Value).Should().OnlyContain(v => v == "—",
            "toplayıcı cevap vermezse gösterge sıfır değil bilinmiyor demeli");
        vm.Kpis.Single(k => k.Key == "fresh").Trend.Should().BeNull();
        vm.Kpis.Single(k => k.Key == "open-work").Url.Should().BeNull();
        vm.Attention.Should().BeEmpty();
        vm.Groups.SelectMany(g => g.Modules).Should().OnlyContain(c => c.AttentionCount == null && c.TotalCount == null);
    }

    [Fact]
    public async Task Sifir_sayac_serit_cizmez_ama_gostergeyi_sifir_gosterir()
    {
        var data = Sample();
        data.UnreadContacts = 0;
        data.PendingReports = 0;
        var sut = BuildSut(data, out _);

        var vm = ViewModelOf(await sut.Index(CancellationToken.None));

        vm.Attention.Should().BeEmpty("sıfır iş varken boş bir şerit dikkat çekmez, dikkat dağıtır");
        var open = vm.Kpis.Single(k => k.Key == "open-work");
        open.Value.Should().Be("0");
        open.Url.Should().BeNull();
    }
}
