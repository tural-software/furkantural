using System.Text.Json;
using FluentAssertions;
using FurkanTural_Admin.Controllers;
using FurkanTural_Admin.Models.Common;
using FurkanTural_Admin.Services;
using FurkanTural_Admin.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FurkanTural_Admin.Tests.Controllers;

/// <summary>Panelin toplu işlem eylemi oturum ister, boş ya da türsüz isteği API'ye taşımaz, kimlikleri tekilleştirip küçük harfli türle iletir ve sonucu tablonun okuduğu üç alanla döner.</summary>
public class BlogControllerBulkTests
{
    private readonly Mock<IBlogApiClient> _client = new();

    private BlogController BuildSut(string? token)
        => new(_client.Object, Mock.Of<ICategoryApiClient>())
        {
            ControllerContext = ControllerTestHelper.BuildControllerContext(token)
        };

    [Fact]
    public async Task Oturum_yoksa_401()
    {
        var result = await BuildSut(null).Bulk("delete", [1], CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
        _client.Verify(c => c.BulkAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<int>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(null, new[] { 1 })]
    [InlineData("delete", new int[0])]
    [InlineData("delete", new[] { 0, -3 })]
    public async Task Tursuz_ya_da_bos_istek_400_ve_API_ye_gitmez(string? action, int[] ids)
    {
        var result = await BuildSut("tok").Bulk(action, ids, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        _client.Verify(c => c.BulkAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<int>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Kimlikler_tekillesir_tur_kucuk_harfe_iner_sonuc_uc_alanla_doner()
    {
        IReadOnlyList<int>? sent = null; string? sentAction = null;
        _client.Setup(c => c.BulkAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<int>>(), "tok", It.IsAny<CancellationToken>()))
            .Callback<string, IReadOnlyList<int>, string, CancellationToken>((a, ids, _, _) => { sentAction = a; sent = ids; })
            .ReturnsAsync(new BulkResultModel { Requested = 3, Affected = 2, Skipped = [9] });

        var result = await BuildSut("tok").Bulk(" Deactivate ", [4, 9, 4, 2], CancellationToken.None);

        sentAction.Should().Be("deactivate");
        sent.Should().Equal(4, 9, 2);
        var root = JsonDocument.Parse(JsonSerializer.Serialize(result.Should().BeOfType<JsonResult>().Which.Value)).RootElement;
        root.GetProperty("requested").GetInt32().Should().Be(3);
        root.GetProperty("affected").GetInt32().Should().Be(2);
        root.GetProperty("skipped")[0].GetInt32().Should().Be(9);
    }

    [Fact]
    public async Task API_cevap_vermezse_500()
    {
        _client.Setup(c => c.BulkAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<int>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BulkResultModel?)null);

        var result = await BuildSut("tok").Bulk("delete", [1], CancellationToken.None);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }
}
