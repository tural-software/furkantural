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

/// <summary>Panelin "Dikkat gerektiren" şeridi ve kart rozetleri iki sayaçtan beslenir: okunmamış iletişim mesajı ve bekleyen şikayet. Her ikisi de silinmişi dışarıda bırakan süzgeçli sayım isteğidir; sayı sıfırsa ne şerit ne rozet çizilir, API cevap vermezse sessizce yok sayılır.</summary>
public class DashboardAttentionTests
{
    private readonly Mock<IContactApiClient> _contacts = new();
    private readonly Mock<IReportApiClient> _reports = new();
    private AdminListRequest? _unreadRequest;
    private AdminListRequest? _pendingRequest;

    private DashboardController BuildSut(int? unread, int? pending)
    {
        _contacts.Setup(c => c.GetAdminCountsAsync(It.IsAny<AdminListRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<AdminListRequest, string, CancellationToken>((r, _, _) => { if (r.Extra("isRead") is not null) _unreadRequest = r; })
            .ReturnsAsync(unread is null ? null : new StatusCountsModel { Total = unread.Value });
        _reports.Setup(c => c.GetAdminCountsAsync(It.IsAny<AdminListRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<AdminListRequest, string, CancellationToken>((r, _, _) => { if (r.Extra("status") is not null) _pendingRequest = r; })
            .ReturnsAsync(pending is null ? null : new StatusCountsModel { Total = pending.Value });

        var url = new Mock<IUrlHelper>();
        url.Setup(u => u.Action(It.IsAny<UrlActionContext>()))
            .Returns<UrlActionContext>(c => "/" + c.Controller + "/" + c.Action);

        return new DashboardController(Mock.Of<IAdminSummaryClient>(), _contacts.Object, _reports.Object, Mock.Of<IUserApiClient>(), Mock.Of<IBlogApiClient>(), Mock.Of<ISubscriberApiClient>())
        {
            ControllerContext = ControllerTestHelper.BuildControllerContext("token"),
            Url = url.Object
        };
    }

    private static DashboardViewModel ViewModelOf(IActionResult result)
        => result.Should().BeOfType<ViewResult>().Which.Model.Should().BeOfType<DashboardViewModel>().Subject;

    [Fact]
    public async Task Sayaclar_silinmisi_disarida_birakan_suzgecli_isteklerle_alinir()
    {
        await BuildSut(3, 2).Index(CancellationToken.None);

        _unreadRequest!.IsDeleted.Should().BeFalse();
        _unreadRequest.Extra("isRead").Should().Be("false", "okunmamış sayısı yalnızca okunmamış satırları saymalı");
        _pendingRequest!.IsDeleted.Should().BeFalse();
        _pendingRequest.Extra("status").Should().Be("Pending");
    }

    [Fact]
    public async Task Sifirdan_buyuk_sayaclar_seride_ve_ilgili_karta_iner()
    {
        var vm = ViewModelOf(await BuildSut(3, 2).Index(CancellationToken.None));

        vm.Attention.Select(a => (a.Slug, a.Count)).Should().Equal(("contact", 3), ("reports", 2));
        vm.Attention.Select(a => a.Url).Should().Equal("/Contact/Index", "/Report/Index");

        var cards = vm.Groups.SelectMany(g => g.Modules).ToDictionary(c => c.Slug);
        cards["contact"].AttentionCount.Should().Be(3);
        cards["contact"].AttentionUrl.Should().Be("/Contact/Index");
        cards["reports"].AttentionCount.Should().Be(2);
        cards.Values.Where(c => c.Slug is not ("contact" or "reports")).Should().OnlyContain(c => c.AttentionCount == null,
            "rozet yalnızca sayacı olan iki modülde görünür; diğer kartlar sessiz kalır");
    }

    [Fact]
    public async Task Sifir_ya_da_cevapsiz_sayac_ne_serit_ne_rozet_cizer()
    {
        var vm = ViewModelOf(await BuildSut(0, null).Index(CancellationToken.None));

        vm.Attention.Should().BeEmpty("sıfır iş varken boş bir şerit dikkat çekmez, dikkat dağıtır");
        vm.Groups.SelectMany(g => g.Modules).Should().OnlyContain(c => c.AttentionCount == null);
    }
}
