using FluentAssertions;
using FurkanTural_Admin.Helpers;
using FurkanTural_Admin.Models.BlogImage;
using FurkanTural_Admin.Models.ChatMessage;
using FurkanTural_Admin.Models.Common;
using FurkanTural_Admin.Models.Log;
using FurkanTural_Admin.Models.Skill;
using FurkanTural_Admin.Services;
using Moq;

namespace FurkanTural_Admin.Tests.Services;

/// <summary>Genel arama yirmi modülü paralel tarar; sözlük ucu olan dört modül kimlikle, diğerleri etiketle ya da arama terimiyle süzülmüş listeye götürür. Her modül en çok beş kayıt ister ve silinmişi dışarıda bırakır; bir modülün istemcisi patlarsa diğerleri yine döner, iki karakterden kısa sorgu hiçbir istemciye gitmez. Şifreli mesaj içeriği aranmaz; mesajlar kullanıcı adıyla bulunur. Görseller dosya adıyla değil önce alternatif metniyle anılır; alternatif metin yoksa dosya adına düşülür.</summary>
public class AdminSearchTests
{
    private readonly Mock<IBlogApiClient> _blogs = new();
    private readonly Mock<ISkillApiClient> _skills = new();
    private readonly Mock<ICategoryApiClient> _categories = new();
    private readonly Mock<IChatMessageApiClient> _messages = new();
    private readonly Mock<IBlogImageApiClient> _blogImages = new();
    private readonly Mock<ILogApiClient> _logs = new();
    private AdminListRequest? _skillRequest;
    private AdminListRequest? _messageRequest;

    private AdminSearch BuildSut()
    {
        _skills.Setup(s => s.GetAdminPagedAsync(It.IsAny<AdminListRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<AdminListRequest, string, CancellationToken>((r, _, _) => _skillRequest = r)
            .ReturnsAsync(((IReadOnlyList<SkillAdminDto>)[new SkillAdminDto { Id = 9, Name = "C#" }, new SkillAdminDto { Id = 10, Name = "" }], 2));
        _categories.Setup(c => c.GetAdminPagedAsync(It.IsAny<AdminListRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("kategori ucu düştü"));
        _messages.Setup(m => m.GetAdminPagedAsync(It.IsAny<AdminListRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<AdminListRequest, string, CancellationToken>((r, _, _) => _messageRequest = r)
            .ReturnsAsync(((IReadOnlyList<ChatMessageAdminDto>)[new ChatMessageAdminDto { Id = 5, SenderUsername = "ali", ReceiverUsername = "veli", MessageType = null }], 1));
        _logs.Setup(l => l.GetAdminPagedAsync(null, null, It.IsAny<string?>(), null, null, 1, AdminSearch.PerModule, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<LogAdminDto>)[new LogAdminDto { Id = 3, Level = "Error", Message = new string('x', 200) }], 1));
        _blogImages.Setup(i => i.GetAdminPagedAsync(It.IsAny<AdminListRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<BlogImageAdminDto>)
            [
                new BlogImageAdminDto { Id = 4, AltText = "EF Core şeması", Url = "uploads/ef-core.png", BlogId = 7 },
                new BlogImageAdminDto { Id = 5, AltText = "  ", Url = "uploads/alt/kapak.webp", BlogId = 8 }
            ], 2));

        return new AdminSearch(
            _blogs.Object, Mock.Of<IMusicApiClient>(), Mock.Of<IProjectApiClient>(), Mock.Of<IRoleApiClient>(),
            _categories.Object, _skills.Object, Mock.Of<IExperienceApiClient>(), Mock.Of<IEducationApiClient>(),
            Mock.Of<IUserApiClient>(), Mock.Of<IContactApiClient>(), Mock.Of<IMailTemplateApiClient>(),
            Mock.Of<ISubscriberApiClient>(), Mock.Of<IStatusApiClient>(),
            Mock.Of<ICallLogApiClient>(), _messages.Object, Mock.Of<IReportApiClient>(),
            _blogImages.Object, Mock.Of<IProjectImageApiClient>(), Mock.Of<IMusicImageApiClient>(), _logs.Object);
    }

    [Fact]
    public async Task Sozluk_modulu_kimlikle_sayfali_modul_etiketle_yonlendirir()
    {
        _blogs.Setup(b => b.GetAdminOptionsAsync("ef core", AdminSearch.PerModule, "tok", It.IsAny<CancellationToken>()))
            .ReturnsAsync([new AdminOptionDto { Id = 7, Label = "EF Core yazısı" }]);

        var groups = await BuildSut().SearchAsync(" ef core ", "tok");

        groups.Select(g => g.Slug).Should().Equal(new[] { "blogs", "skills", "messages", "blog-images", "logs" },
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
    public async Task Mesajlar_icerikle_degil_kullanici_adiyla_aranir_ve_terimle_yonlenir()
    {
        var groups = await BuildSut().SearchAsync("ali", "tok");

        _messageRequest!.Search.Should().BeNull("şifreli içerikte sunucu tarafı arama yok; arama sözcüğü içerik süzgeci olarak gitmez");
        _messageRequest.Extra("username").Should().Be("ali");
        var hit = groups.Single(g => g.Slug == "messages").Hits.Single();
        hit.Label.Should().Be("ali → veli · Text");
        hit.Should().BeEquivalentTo(new { RouteKey = "usernameFilter", RouteValue = "ali" },
            "liste kullanıcı adıyla süzülür; etiket süzgeç değeri olamaz");
    }

    [Fact]
    public async Task Kayit_defteri_mesajla_aranir_etiket_kisaltilir()
    {
        var groups = await BuildSut().SearchAsync("xx", "tok");

        var hit = groups.Single(g => g.Slug == "logs").Hits.Single();
        hit.Label.Should().StartWith("Error · xxx").And.EndWith("…");
        hit.Label.Length.Should().Be(AdminSearch.LabelLength, "uzun kayıt mesajı seçiciyi taşırmamalı");
        hit.RouteKey.Should().Be("searchMessage");
        hit.RouteValue.Should().Be("xx");
    }

    [Fact]
    public async Task Gorsel_once_alternatif_metniyle_yoksa_dosya_adiyla_anilir()
    {
        var groups = await BuildSut().SearchAsync("kapak", "tok");

        var hits = groups.Single(g => g.Slug == "blog-images").Hits;
        hits.Select(h => h.Label).Should().Equal(new[] { "EF Core şeması · Blog #7", "kapak.webp · Blog #8" },
            "alternatif metin insanın yazdığı etikettir, önce o gelir; boşsa yol değil yalnızca dosya adı okunur");
        hits[0].Should().BeEquivalentTo(new { RouteKey = "url", RouteValue = "kapak" },
            "görsel listesinin arama kutusu url alanıdır; sunucu aynı terimi hem adreste hem alternatif metinde arar");
    }

    [Fact]
    public async Task Kisa_sorgu_hicbir_istemciye_gitmez()
    {
        var groups = await BuildSut().SearchAsync("a", "tok");

        groups.Should().BeEmpty();
        _blogs.Verify(b => b.GetAdminOptionsAsync(It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _skills.Verify(s => s.GetAdminPagedAsync(It.IsAny<AdminListRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never,
            "tek harf için yirmi modüle istek açmak her tuş vuruşunda yirmi sorgu demektir");
    }
}
