using FluentAssertions;
using FurkanTural_Admin.Controllers;
using FurkanTural_Admin.Models.DataRetention;
using FurkanTural_Admin.Services;
using FurkanTural_Admin.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FurkanTural_Admin.Tests.Controllers;

/// <summary>Aylık saklama temizliği kalıcı silme yapar. Onay tarayıcıda zorunlu tutulsa da sunucu da denetler; elle kurulan bir istek onay olmadan hiçbir şey sildiremez.</summary>
public class DataRetentionControllerTests
{
    private static (DataRetentionController Sut, Mock<IDataRetentionApiClient> Api) Build(string? token = "jwt")
    {
        var api = new Mock<IDataRetentionApiClient>();
        api.Setup(a => a.PreviewAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DataRetentionReportModel { Categories = [new DataRetentionCategoryModel { Key = "logs", Label = "Kayıtlar", Count = 3 }] });
        api.Setup(a => a.PurgeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DataRetentionReportModel { Categories = [new DataRetentionCategoryModel { Key = "logs", Label = "Kayıtlar", Count = 3 }] });

        var sut = new DataRetentionController(api.Object) { ControllerContext = ControllerTestHelper.BuildControllerContext(token) };
        return (sut, api);
    }

    [Fact]
    public async Task Sayfa_yalnizca_onizler_hicbir_sey_silmez()
    {
        var (sut, api) = Build();

        var view = (ViewResult)await sut.Index();

        view.Model.Should().BeOfType<DataRetentionViewModel>().Which.Preview!.Total.Should().Be(3);
        api.Verify(a => a.PurgeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Onay_kutusu_isaretlenmeden_silme_yapilmaz()
    {
        var (sut, api) = Build();

        var view = (ViewResult)await sut.Purge(confirm: false);

        view.Model.Should().BeOfType<DataRetentionViewModel>().Which.Error.Should().Contain("onay");
        api.Verify(a => a.PurgeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never,
            "kutu yalnızca tarayıcıda zorunlu olsaydı elle kurulan istek onu atlardı");
    }

    [Fact]
    public async Task Onaylanan_istek_siler_ve_sonucu_gosterir()
    {
        var (sut, api) = Build();

        var view = (ViewResult)await sut.Purge(confirm: true);

        view.Model.Should().BeOfType<DataRetentionViewModel>().Which.Purged!.Total.Should().Be(3);
        api.Verify(a => a.PurgeAsync("jwt", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Oturumsuz_istek_girise_yonlenir()
    {
        var (sut, api) = Build(token: null);

        (await sut.Purge(confirm: true)).Should().BeOfType<RedirectToActionResult>();
        api.Verify(a => a.PurgeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
