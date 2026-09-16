using System.Text;
using FluentAssertions;
using FurkanTural_Admin.Controllers;
using FurkanTural_Admin.Models.Common;
using FurkanTural_Admin.Services;
using FurkanTural_Admin.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;

namespace FurkanTural_Admin.Tests.Controllers;

/// <summary>Paneldeki ek vekili. Yukarıdan gelen içerik türüne güvenilmez: tür uzantıdan, sabit bir eşlemeden belirlenir. Aksi hâlde yanlış etiketlenmiş bir dosya panelin kendi kökeninde HTML olarak açılabilirdi.</summary>
public class ChatAttachmentProxyTests
{
    private static (ChatMessageController Sut, Mock<IChatMessageApiClient> Api) Build(string upstreamType)
    {
        var api = new Mock<IChatMessageApiClient>();
        api.Setup(a => a.GetAttachmentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((Stream?)new MemoryStream(Encoding.UTF8.GetBytes("<script>alert(1)</script>")), (string?)upstreamType));

        var sut = new ChatMessageController(api.Object, Options.Create(new ApiOptions()))
        {
            ControllerContext = ControllerTestHelper.BuildControllerContext("jwt")
        };

        return (sut, api);
    }

    [Theory]
    [InlineData("resim.png", "image/png")]
    [InlineData("ses.mp3", "audio/mpeg")]
    [InlineData("video.mp4", "video/mp4")]
    public async Task Icerik_turu_yukaridan_degil_uzantidan_belirlenir(string file, string expected)
    {
        var (sut, _) = Build(upstreamType: "text/html");

        var result = await sut.Attachment(file);

        result.Should().BeOfType<FileStreamResult>().Which.ContentType.Should().Be(expected,
            "yukarıdan gelen text/html kabul edilseydi dosya panelin kökeninde betik olarak çalışabilirdi");
        sut.Response.Headers.XContentTypeOptions.ToString().Should().Be("nosniff");
        sut.Response.Headers.ContentDisposition.ToString().Should().Be("inline");
    }

    [Theory]
    [InlineData("sayfa.html")]
    [InlineData("betik.svg")]
    [InlineData("uzantisiz")]
    public async Task Eslemede_olmayan_uzanti_istenmeden_reddedilir(string file)
    {
        var (sut, api) = Build(upstreamType: "image/png");

        var result = await sut.Attachment(file);

        result.Should().BeOfType<BadRequestResult>();
        api.Verify(a => a.GetAttachmentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
