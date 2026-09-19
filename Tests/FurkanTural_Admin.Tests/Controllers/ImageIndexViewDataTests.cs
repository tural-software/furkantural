using FluentAssertions;
using FurkanTural_Admin.Controllers;
using FurkanTural_Admin.Helpers;
using FurkanTural_Admin.Models.BlogImage;
using FurkanTural_Admin.Models.MusicImage;
using FurkanTural_Admin.Models.ProjectImage;
using FurkanTural_Admin.Services;
using FurkanTural_Admin.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;

namespace FurkanTural_Admin.Tests.Controllers;

public class ImageIndexViewDataTests
{
    private static readonly IConfiguration Config = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["Api:BaseUrl"] = "https://api.example.test/" })
        .Build();

    [Fact]
    public async Task Blog_gorseli_Index_API_kokunu_ve_secili_blogu_gorunume_tasir()
    {
        var client = new Mock<IBlogImageApiClient>();
        client.Setup(c => c.GetAdminPagedAsync(It.IsAny<AdminListRequest>(), "tok", It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<BlogImageAdminDto>)[], 0));
        var sut = new BlogImageController(client.Object, Mock.Of<IBlogApiClient>(), Config)
        {
            ControllerContext = ControllerTestHelper.BuildControllerContext("tok")
        };

        var view = (await sut.Index(null, null, null, null, 18, null, null)).Should().BeOfType<ViewResult>().Which;

        view.ViewData["ApiBaseUrl"].Should().Be("https://api.example.test");
        view.Model.Should().BeOfType<BlogImageIndexViewModel>().Which.BlogIdFilter.Should().Be(18);
    }

    [Fact]
    public async Task Muzik_gorseli_Index_API_kokunu_ve_secili_muzigi_gorunume_tasir()
    {
        var client = new Mock<IMusicImageApiClient>();
        client.Setup(c => c.GetAdminPagedAsync(It.IsAny<AdminListRequest>(), "tok", It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<MusicImageAdminDto>)[], 0));
        var sut = new MusicImageController(client.Object, Mock.Of<IMusicApiClient>(), Config)
        {
            ControllerContext = ControllerTestHelper.BuildControllerContext("tok")
        };

        var view = (await sut.Index(null, null, null, null, 7, null, null)).Should().BeOfType<ViewResult>().Which;

        view.ViewData["ApiBaseUrl"].Should().Be("https://api.example.test");
        view.Model.Should().BeOfType<MusicImageIndexViewModel>().Which.MusicIdFilter.Should().Be(7);
    }

    [Fact]
    public async Task Proje_gorseli_Index_API_kokunu_ve_secili_projeyi_gorunume_tasir()
    {
        var client = new Mock<IProjectImageApiClient>();
        client.Setup(c => c.GetAdminPagedAsync(It.IsAny<AdminListRequest>(), "tok", It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<ProjectImageAdminDto>)[], 0));
        var sut = new ProjectImageController(client.Object, Mock.Of<IProjectApiClient>(), Config)
        {
            ControllerContext = ControllerTestHelper.BuildControllerContext("tok")
        };

        var view = (await sut.Index(null, null, null, null, 3, null, null)).Should().BeOfType<ViewResult>().Which;

        view.ViewData["ApiBaseUrl"].Should().Be("https://api.example.test");
        view.Model.Should().BeOfType<ProjectImageIndexViewModel>().Which.ProjectIdFilter.Should().Be(3);
    }
}
