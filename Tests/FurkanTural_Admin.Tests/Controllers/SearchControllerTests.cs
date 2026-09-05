using System.Text.Json;
using FluentAssertions;
using FurkanTural_Admin.Controllers;
using FurkanTural_Admin.Services;
using FurkanTural_Admin.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Moq;

namespace FurkanTural_Admin.Tests.Controllers;

/// <summary>Arama ucu oturum ister ve her kaydı modülün süzülmüş liste adresine çevirir; JSON alan adları modül seçicinin okuduğu adlardır (query, groups, slug, title, items, id, label, url).</summary>
public class SearchControllerTests
{
    private static SearchController BuildSut(string? token, IReadOnlyList<SearchGroup> groups)
    {
        var search = new Mock<IAdminSearch>();
        search.Setup(s => s.SearchAsync(It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(groups);

        var url = new Mock<IUrlHelper>();
        url.Setup(u => u.Action(It.IsAny<UrlActionContext>()))
            .Returns<UrlActionContext>(c =>
            {
                var values = new RouteValueDictionary(c.Values);
                var pair = values.First();
                return "/" + c.Controller + "?" + pair.Key + "=" + Uri.EscapeDataString(pair.Value?.ToString() ?? "");
            });

        return new SearchController(search.Object)
        {
            ControllerContext = ControllerTestHelper.BuildControllerContext(token),
            Url = url.Object
        };
    }

    [Fact]
    public async Task Oturum_yoksa_401()
    {
        var result = await BuildSut(null, []).Index("ef", CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task Kayitlar_modulun_suzulmus_liste_adresine_cevrilir()
    {
        var groups = new List<SearchGroup>
        {
            new("blogs", "Blog Yazıları", "Blog", [new SearchHit(7, "EF Core", "blogId", "7")]),
            new("skills", "Beceriler", "Skill", [new SearchHit(9, "C#", "name", "C#")])
        };

        var result = await BuildSut("tok", groups).Index(" ef ", CancellationToken.None);

        var json = JsonSerializer.Serialize(result.Should().BeOfType<JsonResult>().Which.Value);
        var root = JsonDocument.Parse(json).RootElement;
        root.GetProperty("query").GetString().Should().Be("ef");
        var first = root.GetProperty("groups")[0];
        first.GetProperty("slug").GetString().Should().Be("blogs");
        first.GetProperty("title").GetString().Should().Be("Blog Yazıları");
        first.GetProperty("items")[0].GetProperty("url").GetString().Should().Be("/Blog?blogId=7");
        root.GetProperty("groups")[1].GetProperty("items")[0].GetProperty("url").GetString().Should().Be("/Skill?name=C%23",
            "etiket adres satırına kaçışlı girer; # işareti kaçışsız kalırsa süzgeç boş gelir");
    }
}
