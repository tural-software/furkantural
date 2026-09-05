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

/// <summary>Panel başlığındaki dört gösterge var olan sayaç uçlarından türetilir. "Bu hafta yeni" dört modülün son yedi günü toplar ve önceki yedi güne göre farkı ok olarak taşır; "Aktif kullanıcı" seenSince süzgeciyle gelir; sayaç cevap vermezse gösterge tire kalır, uydurma sıfır yazılmaz.</summary>
public class DashboardKpiTests
{
    private static readonly DateTime Today = DateTime.UtcNow.Date;

    private static void Answer<T>(Mock<T> mock, Func<AdminListRequest, int?> answer) where T : class
    {
        switch (mock)
        {
            case Mock<IUserApiClient> u:
                u.Setup(c => c.GetAdminCountsAsync(It.IsAny<AdminListRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((AdminListRequest r, string _, CancellationToken _) => Wrap(answer(r)));
                break;
            case Mock<IBlogApiClient> b:
                b.Setup(c => c.GetAdminCountsAsync(It.IsAny<AdminListRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((AdminListRequest r, string _, CancellationToken _) => Wrap(answer(r)));
                break;
            case Mock<IContactApiClient> c:
                c.Setup(x => x.GetAdminCountsAsync(It.IsAny<AdminListRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((AdminListRequest r, string _, CancellationToken _) => Wrap(answer(r)));
                break;
            case Mock<ISubscriberApiClient> s:
                s.Setup(x => x.GetAdminCountsAsync(It.IsAny<AdminListRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((AdminListRequest r, string _, CancellationToken _) => Wrap(answer(r)));
                break;
            case Mock<IReportApiClient> rep:
                rep.Setup(x => x.GetAdminCountsAsync(It.IsAny<AdminListRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((AdminListRequest r, string _, CancellationToken _) => Wrap(answer(r)));
                break;
        }
    }

    private static StatusCountsModel? Wrap(int? total) => total is null ? null : new StatusCountsModel { Total = total.Value };

    private static bool ThisWeek(AdminListRequest r) => r.DateFrom == Today.AddDays(-6) && r.DateTo is null && r.Extra("seenSince") is null;
    private static bool LastWeek(AdminListRequest r) => r.DateFrom == Today.AddDays(-13) && r.DateTo == Today.AddDays(-7);

    private static DashboardController BuildSut(
        Func<AdminListRequest, int?> users, Func<AdminListRequest, int?> blogs,
        Func<AdminListRequest, int?> contacts, Func<AdminListRequest, int?> subscribers,
        Func<AdminListRequest, int?> reports)
    {
        var u = new Mock<IUserApiClient>(); Answer(u, users);
        var b = new Mock<IBlogApiClient>(); Answer(b, blogs);
        var c = new Mock<IContactApiClient>(); Answer(c, contacts);
        var s = new Mock<ISubscriberApiClient>(); Answer(s, subscribers);
        var r = new Mock<IReportApiClient>(); Answer(r, reports);

        var url = new Mock<IUrlHelper>();
        url.Setup(x => x.Action(It.IsAny<UrlActionContext>())).Returns<UrlActionContext>(x => "/" + x.Controller);

        return new DashboardController(Mock.Of<IAdminSummaryClient>(), c.Object, r.Object, u.Object, b.Object, s.Object)
        {
            ControllerContext = ControllerTestHelper.BuildControllerContext("token"),
            Url = url.Object
        };
    }

    private static IReadOnlyList<KpiViewModel> KpisOf(IActionResult result)
        => result.Should().BeOfType<ViewResult>().Which.Model.Should().BeOfType<DashboardViewModel>().Subject.Kpis;

    [Fact]
    public async Task Bu_hafta_yeni_dort_modulu_toplar_ve_gecen_haftaya_gore_fark_verir()
    {
        var sut = BuildSut(
            users: r => r.Extra("seenSince") is not null ? 4 : ThisWeek(r) ? 5 : LastWeek(r) ? 2 : 0,
            blogs: r => ThisWeek(r) ? 2 : LastWeek(r) ? 1 : 0,
            contacts: r => r.Extra("isRead") is not null ? 3 : ThisWeek(r) ? 3 : LastWeek(r) ? 0 : 0,
            subscribers: r => ThisWeek(r) ? 1 : LastWeek(r) ? 1 : 0,
            reports: r => r.Extra("status") is not null ? 2 : 0);

        var kpis = KpisOf(await sut.Index(CancellationToken.None));

        kpis.Select(k => k.Key).Should().Equal(new[] { "records", "fresh", "active-users", "open-work" });
        var fresh = kpis.Single(k => k.Key == "fresh");
        fresh.Value.Should().Be("11");
        fresh.Detail.Should().Be("2 yazı · 5 kullanıcı · 3 mesaj · 1 abone");
        fresh.Trend.Should().Be(7, "bu hafta 11, geçen hafta 4; ok yukarı ve fark yedi");
        fresh.TrendText.Should().Be("geçen hafta 4");
        kpis.Single(k => k.Key == "active-users").Value.Should().Be("4");
        var open = kpis.Single(k => k.Key == "open-work");
        open.Value.Should().Be("5");
        open.Detail.Should().Be("3 okunmamış mesaj · 2 bekleyen şikayet");
        open.Url.Should().Be("#dash-attention-title", "bekleyen iş göstergesi ayrıntıları veren şeride atlar");
    }

    [Fact]
    public async Task Aktif_kullanici_son_yedi_gunun_baslangicini_seenSince_ile_ister()
    {
        AdminListRequest? seen = null;
        var sut = BuildSut(
            users: r => { if (r.Extra("seenSince") is not null) seen = r; return 1; },
            blogs: _ => 0, contacts: _ => 0, subscribers: _ => 0, reports: _ => 0);

        await sut.Index(CancellationToken.None);

        seen.Should().NotBeNull();
        seen!.Extra("seenSince").Should().Be(Today.AddDays(-6).ToString("yyyy-MM-dd"));
        seen.IsDeleted.Should().BeFalse();
        seen.IsActive.Should().BeTrue("kapatılmış hesap son görülme tarihi taze olsa da aktif sayılmaz");
    }

    [Fact]
    public async Task Cevapsiz_sayac_tire_kalir_sifir_uydurulmaz()
    {
        var sut = BuildSut(users: _ => null, blogs: _ => null, contacts: _ => null, subscribers: _ => null, reports: _ => null);

        var kpis = KpisOf(await sut.Index(CancellationToken.None));

        kpis.Select(k => k.Value).Should().OnlyContain(v => v == "—",
            "özet hizmeti ve sayaçlar cevap vermezse gösterge sıfır değil bilinmiyor demeli");
        kpis.Single(k => k.Key == "fresh").Trend.Should().BeNull();
        kpis.Single(k => k.Key == "open-work").Url.Should().BeNull();
    }
}
