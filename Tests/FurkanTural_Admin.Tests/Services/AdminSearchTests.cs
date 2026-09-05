using FluentAssertions;
using FurkanTural_Admin.Helpers;
using FurkanTural_Admin.Models.Common;
using FurkanTural_Admin.Models.Skill;
using FurkanTural_Admin.Services;
using Moq;

namespace FurkanTural_Admin.Tests.Services;

/// <summary>Genel arama on üç modülü paralel tarar; sözlük ucu olan dört modül kimlikle, diğerleri etiketle süzülmüş listeye götürür. Her modül en çok beş kayıt ister ve silinmişi dışarıda bırakır; bir modülün istemcisi patlarsa diğerleri yine döner, iki karakterden kısa sorgu hiçbir istemciye gitmez.</summary>
public class AdminSearchTests
{
    private readonly Mock<IBlogApiClient> _blogs = new();
    private readonly Mock<ISkillApiClient> _skills = new();
    private readonly Mock<ICategoryApiClient> _categories = new();
    private AdminListRequest? _skillRequest;

    private AdminSearch BuildSut()
    {
        _skills.Setup(s => s.GetAdminPagedAsync(It.IsAny<AdminListRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<AdminListRequest, string, CancellationToken>((r, _, _) => _skillRequest = r)
            .ReturnsAsync(((IReadOnlyList<SkillAdminDto>)[new SkillAdminDto { Id = 9, Name = "C#" }, new SkillAdminDto { Id = 10, Name = "" }], 2));
        _categories.Setup(c => c.GetAdminPagedAsync(It.IsAny<AdminListRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("kategori ucu düştü"));

        return new AdminSearch(
            _blogs.Object, Mock.Of<IMusicApiClient>(), Mock.Of<IProjectApiClient>(), Mock.Of<IRoleApiClient>(),
            _categories.Object, _skills.Object, Mock.Of<IExperienceApiClient>(), Mock.Of<IEducationApiClient>(),
            Mock.Of<IUserApiClient>(), Mock.Of<IContactApiClient>(), Mock.Of<IMailTemplateApiClient>(),
            Mock.Of<ISubscriberApiClient>(), Mock.Of<IStatusApiClient>());
    }

    [Fact]
    public async Task Sozluk_modulu_kimlikle_sayfali_modul_etiketle_yonlendirir()
    {
        _blogs.Setup(b => b.GetAdminOptionsAsync("ef core", AdminSearch.PerModule, "tok", It.IsAny<CancellationToken>()))
            .ReturnsAsync([new AdminOptionDto { Id = 7, Label = "EF Core yazısı" }]);

        var groups = await BuildSut().SearchAsync(" ef core ", "tok");

        groups.Select(g => g.Slug).Should().Equal(new[] { "blogs", "skills" },
            "kategori ucu patladı ama bu yalnızca o grubu düşürür; sıra modül kaydındaki sıradır");
        groups[0].Hits.Should().Equal(new SearchHit(7, "EF Core yazısı", "blogId", "7"));
        groups[1].Controller.Should().Be("Skill");
        groups[1].Hits.Should().Equal(new[] { new SearchHit(9, "C#", "name", "C#") },
            "etiketi boş satır listeye götüremez, atlanır");
    }

    [Fact]
    public async Task Sayfali_istek_bes_satir_ister_ve_silinmisi_disarida_birakir()
    {
        await BuildSut().SearchAsync("c#", "tok");

        _skillRequest!.Search.Should().Be("c#");
        _skillRequest.PageSize.Should().Be(AdminSearch.PerModule);
        _skillRequest.PageNumber.Should().Be(1);
        _skillRequest.IsDeleted.Should().BeFalse("silinmiş kayda atlamak yöneticiyi şaşırtır; sözlük ucu da silinmişi vermez");
    }

    [Fact]
    public async Task Kisa_sorgu_hicbir_istemciye_gitmez()
    {
        var groups = await BuildSut().SearchAsync("a", "tok");

        groups.Should().BeEmpty();
        _blogs.Verify(b => b.GetAdminOptionsAsync(It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _skills.Verify(s => s.GetAdminPagedAsync(It.IsAny<AdminListRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never,
            "tek harf için on üç modüle istek açmak her tuş vuruşunda on üç sorgu demektir");
    }
}
