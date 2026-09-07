using FluentAssertions;
using FurkanTural_Blog.Controllers;
using FurkanTural_Blog.Models;
using FurkanTural_Blog.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Configuration;
using Moq;

namespace FurkanTural_Blog.Tests;

/// <summary>Yorum gönderiminin sayfa tarafı. İki yol bilinçli olarak farklıdır: başarıda yönlendirme yapılır — tarayıcının yenile tuşu ikinci bir yorum kaydı açmasın diye — başarısızlıkta ise sayfa yeniden çizilir ve yazılan metin formda kalır.<para>Tuzak alan doluysa istek API'ye hiç çıkmaz ama ekranda başarı görünür: bota tuzağa düştüğünü söylemek, tuzağı bir sonraki denemede işe yaramaz hâle getirir.</para></summary>
public class CommentSubmitTests
{
    private readonly Mock<IBlogApiService> _api = new();
    private readonly Mock<ICommentClient> _comments = new();

    public CommentSubmitTests()
    {
        _api.Setup(a => a.GetPostBySlugAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BlogPostViewModel { Id = 7, Slug = "ornek", Title = "Başlık", Content = "İçerik" });
        _api.Setup(a => a.GetImagesByBlogAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _comments.Setup(c => c.GetThreadAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommentThreadViewModel());
        _comments.Setup(c => c.SubmitAsync(It.IsAny<CommentFormModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommentOutcome(true, "Yorumunuz alındı."));
    }

    private HomeController Build()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Api:BaseUrl"] = "https://api.test" })
            .Build();

        return new HomeController(_api.Object, _comments.Object, Mock.Of<IAppConfigService>(), config);
    }

    private static CommentFormModel Filled() => new()
    {
        BlogId = 7,
        AuthorName = "Okur",
        AuthorEmail = "okur@site.test",
        Body = "Yazı için teşekkürler.",
        TurnstileToken = "jeton"
    };

    [Fact]
    public async Task Bulunamayan_yaziya_yorum_birakilamaz()
    {
        _api.Setup(a => a.GetPostBySlugAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BlogPostViewModel?)null);

        var result = await Build().Comment("yok", Filled(), default);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Basarili_gonderim_yaziya_geri_yonlendirir()
    {
        var result = await Build().Comment("ornek", Filled(), default);

        var redirect = result.Should().BeOfType<RedirectToRouteResult>().Subject;
        redirect.RouteName.Should().Be("BlogPost");
        redirect.RouteValues!["slug"].Should().Be("ornek");
        redirect.RouteValues!["yorum"].Should().Be("alindi");
    }

    [Fact]
    public async Task Tuzak_alan_doluysa_istek_apiye_cikmaz()
    {
        var form = Filled();
        form.Website = "https://spam.example";

        var result = await Build().Comment("ornek", form, default);

        result.Should().BeOfType<RedirectToRouteResult>("bota tuzağa düştüğü söylenmez");
        _comments.Verify(c => c.SubmitAsync(It.IsAny<CommentFormModel>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Bot_jetonu_yoksa_istek_aga_cikmadan_elenir()
    {
        var form = Filled();
        form.TurnstileToken = null;

        var result = await Build().Comment("ornek", form, default);

        var view = result.Should().BeOfType<ViewResult>().Subject;
        var model = view.Model.Should().BeOfType<BlogPostViewModel>().Subject;
        model.CommentForm.Succeeded.Should().BeFalse();
        model.CommentForm.ResultMessage.Should().Contain("Bot doğrulaması");
        _comments.Verify(c => c.SubmitAsync(It.IsAny<CommentFormModel>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Dogrulama_hatasinda_yazilan_metin_formda_kalir()
    {
        var controller = Build();
        controller.ModelState.AddModelError("Body", "Yorumunuzu yazın.");

        var result = await controller.Comment("ornek", Filled(), default);

        var model = result.Should().BeOfType<ViewResult>().Subject.Model.Should().BeOfType<BlogPostViewModel>().Subject;
        model.CommentForm.Body.Should().Be("Yazı için teşekkürler.",
            "doğrulama hatası yüzünden kullanıcının yazdığını silmek, formu ikinci kez doldurmaya zorlamak olurdu");
        model.CommentForm.Submitted.Should().BeTrue();
        model.CommentForm.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Api_reddederse_kendi_metni_gosterilir()
    {
        _comments.Setup(c => c.SubmitAsync(It.IsAny<CommentFormModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommentOutcome(false, "Bir yanıta yanıt verilemez."));

        var result = await Build().Comment("ornek", Filled(), default);

        var model = result.Should().BeOfType<ViewResult>().Subject.Model.Should().BeOfType<BlogPostViewModel>().Subject;
        model.CommentForm.ResultMessage.Should().Be("Bir yanıta yanıt verilemez.",
            "API'nin cümlesi kullanıcıya daha çok şey söyler; genel bir metne indirmek sebebi gizlerdi");
    }

    [Fact]
    public async Task Api_metinsiz_reddederse_varsayilan_gosterilir()
    {
        _comments.Setup(c => c.SubmitAsync(It.IsAny<CommentFormModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommentOutcome(false, null));

        var result = await Build().Comment("ornek", Filled(), default);

        var model = result.Should().BeOfType<ViewResult>().Subject.Model.Should().BeOfType<BlogPostViewModel>().Subject;
        model.CommentForm.ResultMessage.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Yazi_sayfasi_yorumlari_ve_bos_formu_tasir()
    {
        _comments.Setup(c => c.GetThreadAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommentThreadViewModel
            {
                BlogId = 7,
                TotalCount = 2,
                RootCount = 1,
                Items = [new CommentViewModel { Id = 1, AuthorName = "Okur", Body = "Merhaba" }]
            });

        var result = await Build().Detail("ornek", null, default);

        var model = result.Should().BeOfType<ViewResult>().Subject.Model.Should().BeOfType<BlogPostViewModel>().Subject;
        model.Comments.TotalCount.Should().Be(2);
        model.CommentForm.BlogId.Should().Be(7);
        model.CommentForm.Submitted.Should().BeFalse();
    }

    [Fact]
    public async Task Yonlendirmeden_donen_sayfa_onay_metnini_gosterir()
    {
        var result = await Build().Detail("ornek", "alindi", default);

        var model = result.Should().BeOfType<ViewResult>().Subject.Model.Should().BeOfType<BlogPostViewModel>().Subject;
        model.CommentForm.Submitted.Should().BeTrue();
        model.CommentForm.Succeeded.Should().BeTrue();
        model.CommentForm.ResultMessage.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Bilinmeyen_isaret_onay_metni_uretmez()
    {
        var result = await Build().Detail("ornek", "uydurma", default);

        var model = result.Should().BeOfType<ViewResult>().Subject.Model.Should().BeOfType<BlogPostViewModel>().Subject;
        model.CommentForm.Submitted.Should().BeFalse("adres satırına elle yazılan bir değer, gönderilmemiş bir yorumu gönderilmiş göstermemeli");
    }
}
