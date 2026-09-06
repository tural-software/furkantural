using System.Security.Claims;
using FluentAssertions;
using FurkanTural_API.Controllers;
using FurkanTural_API.Models.Log;
using FurkanTural_Application.DTOs.Log;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FurkanTural_API.Tests;

/// <summary>Kaynak adının ilk parçası — uygulama — her zaman sunucudan gelir: istemci logunda <c>app_source</c> claim'inden, panel logunda sabit olarak. Gövde yalnızca bileşen ve işlem parçalarını söyleyebilir.<para>Bu ayrım güvenlik özelliğidir, biçim tercihi değil: gövde uygulama adını da belirleyebilseydi bir ön-yüz kendi hatasını başka bir uygulamanın kaydı gibi yazdırabilir, denetim izini kirletebilirdi.</para></summary>
public class LogSourceEndpointTests
{
    private static (Mock<ILogService> Service, Func<CreateLogDto?> Written) Service()
    {
        CreateLogDto? written = null;
        var service = new Mock<ILogService>();
        service.Setup(s => s.CreateAsync(It.IsAny<CreateLogDto>(), It.IsAny<CancellationToken>()))
            .Callback<CreateLogDto, CancellationToken>((d, _) => written = d)
            .ReturnsAsync(Result<LogDto>.Ok(new LogDto()));
        return (service, () => written);
    }

    private static ControllerContext ContextWith(params Claim[] claims)
    {
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")) };
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;
        return new ControllerContext { HttpContext = context };
    }

    [Fact]
    public async Task Istemci_logunda_uygulama_adi_claimden_gelir()
    {
        var (service, written) = Service();
        var sut = new ClientLogController(service.Object, Mock.Of<IClock>())
        {
            ControllerContext = ContextWith(new Claim("app_source", "Chat"))
        };

        var result = await sut.Create(new ClientLogRequest { Message = "Bağlantı koptu", Component = "Chat-Detail" }, default);

        result.Should().BeOfType<NoContentResult>();
        written()!.Source.Should().Be("FurkanTural_Chat-Chat-Detail");
    }

    [Fact]
    public async Task Istemci_govdesi_uygulama_adini_ele_geciremez()
    {
        var (service, written) = Service();
        var sut = new ClientLogController(service.Object, Mock.Of<IClock>())
        {
            ControllerContext = ContextWith(new Claim("app_source", "Blog"))
        };

        await sut.Create(new ClientLogRequest { Message = "x", Component = "FurkanTural_Admin-Users-Delete" }, default);

        written()!.Source.Should().Be("FurkanTural_Blog-Admin-Users-Delete",
            "ilk parça claim'in söylediğidir ve oynanamaz; gövdeden gelen metin uygulama adına da benzeyemez, yoksa o adla yapılan arama bu satırı getirirdi");
    }

    [Fact]
    public async Task Claimsiz_istemci_logu_yazilmaz()
    {
        var (service, _) = Service();
        var sut = new ClientLogController(service.Object, Mock.Of<IClock>()) { ControllerContext = ContextWith() };

        await sut.Create(new ClientLogRequest { Message = "x" }, default);

        service.Verify(s => s.CreateAsync(It.IsAny<CreateLogDto>(), It.IsAny<CancellationToken>()), Times.Never,
            "kaynağı bilinmeyen kayıt hiç yazılmamalı; bilinen bir uygulamaya yazmak yanlış iz bırakırdı");
    }

    [Fact]
    public async Task Panel_logu_her_zaman_admin_olarak_damgalanir()
    {
        var (service, written) = Service();
        var sut = new LogController(service.Object, Mock.Of<IClock>()) { ControllerContext = ContextWith() };

        await sut.Create(new CreateLogRequest { Message = "PUT /api/v1/blog -> 500", Component = "Blog-Update-Post" }, default);

        written()!.Source.Should().Be("FurkanTural_Admin-Blog-Update-Post");
    }

    [Fact]
    public async Task Bilesen_bos_gelirse_kaynak_yalnizca_uygulama_adidir()
    {
        var (service, written) = Service();
        var sut = new LogController(service.Object, Mock.Of<IClock>()) { ControllerContext = ContextWith() };

        await sut.Create(new CreateLogRequest { Message = "x" }, default);

        written()!.Source.Should().Be("FurkanTural_Admin", "bilinmeyen bileşen uydurulmaz, boş bırakılır");
    }
}
