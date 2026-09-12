using FluentAssertions;
using FurkanTural_Blog.Controllers;
using FurkanTural_Blog.Models;
using FurkanTural_Blog.Services;
using Microsoft.AspNetCore.Http;
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

    private HomeController Build(bool scripted = false)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Api:BaseUrl"] = "https://api.test" })
            .Build();

        var http = new DefaultHttpContext();
        if (scripted)
            http.Request.Headers.XRequestedWith = "XMLHttpRequest";

        return new HomeController(_api.Object, _comments.Object, Mock.Of<IAppConfigService>(), config)
        {
            ControllerContext = new ControllerContext { HttpContext = http }
        };
    }

    private static Dictionary<string, string>? ErrorsOf(object payload) =>
        payload.GetType().GetProperty("errors")?.GetValue(payload) as Dictionary<string, string>;

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
            .ReturnsAsync(new CommentOutcome(false, "Yanıtlanan yorum bulunamadı."));

        var result = await Build().Comment("ornek", Filled(), default);

        var model = result.Should().BeOfType<ViewResult>().Subject.Model.Should().BeOfType<BlogPostViewModel>().Subject;
        model.CommentForm.ResultMessage.Should().Be("Yanıtlanan yorum bulunamadı.",
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
    public async Task Betikli_gonderim_yonlendirme_yerine_json_doner()
    {
        var result = await Build(scripted: true).Comment("ornek", Filled(), default);

        var payload = result.Should().BeOfType<JsonResult>().Subject.Value!;
        payload.Should().BeEquivalentTo(new { ok = true },
            options => options.ExcludingMissingMembers());
    }

    [Fact]
    public async Task Betikli_gonderimde_yazi_sayfasi_yeniden_cizilmez()
    {
        await Build(scripted: true).Comment("ornek", Filled(), default);

        _api.Verify(a => a.GetImagesByBlogAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never,
            "sayfa yerinde durduğu için kapak, ilgili yazı ve yorum listesi yeniden çekilmemeli");
        _comments.Verify(c => c.GetThreadAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Betikli_dogrulama_hatasi_alan_adiyla_birlikte_doner()
    {
        var controller = Build(scripted: true);
        controller.ModelState.AddModelError("Body", "Yorumunuzu yazın.");

        var result = await controller.Comment("ornek", Filled(), default);

        var payload = result.Should().BeOfType<JsonResult>().Subject.Value!;
        payload.Should().BeEquivalentTo(new { ok = false }, options => options.ExcludingMissingMembers());

        var errors = ErrorsOf(payload);
        errors.Should().NotBeNull();
        errors!["Body"].Should().Be("Yorumunuzu yazın.",
            "hata alan adıyla döner ki sayfa onu doğru kutunun altına yazabilsin");
        _comments.Verify(c => c.SubmitAsync(It.IsAny<CommentFormModel>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Betikli_istekte_api_reddi_kendi_metniyle_doner()
    {
        _comments.Setup(c => c.SubmitAsync(It.IsAny<CommentFormModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommentOutcome(false, "Yanıtlanan yorum bulunamadı."));

        var result = await Build(scripted: true).Comment("ornek", Filled(), default);

        var payload = result.Should().BeOfType<JsonResult>().Subject.Value!;
        payload.Should().BeEquivalentTo(new { ok = false, message = "Yanıtlanan yorum bulunamadı." },
            options => options.ExcludingMissingMembers());
    }

    [Fact]
    public async Task Betikli_istekte_tuzak_alan_yine_basari_gosterir()
    {
        var form = Filled();
        form.Website = "https://spam.example";

        var result = await Build(scripted: true).Comment("ornek", form, default);

        var payload = result.Should().BeOfType<JsonResult>().Subject.Value!;
        payload.Should().BeEquivalentTo(new { ok = true }, options => options.ExcludingMissingMembers());
        _comments.Verify(c => c.SubmitAsync(It.IsAny<CommentFormModel>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Basliksiz_gonderim_javascriptsiz_yolu_surer()
    {
        var result = await Build(scripted: false).Comment("ornek", Filled(), default);

        result.Should().BeOfType<RedirectToRouteResult>(
            "başlığı yalnızca sayfanın kendi betiği koyar; başka yoldan gelen gönderim eski yolu izler");
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
