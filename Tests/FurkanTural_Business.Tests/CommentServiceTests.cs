using System.Linq.Expressions;
using FluentAssertions;
using FurkanTural_Application.DTOs.Comment;
using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Business.Helpers;
using FurkanTural_Business.Services.Concrete;
using FurkanTural_Domain.Constants;
using FurkanTural_Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FurkanTural_Business.Tests;

/// <summary>Yorum servisinin dört sözü sınanır: okura yalnızca onaylanmış satır çizilir, adres hiçbir okumada dışarı çıkmaz, bildirim yalnızca isteyene ve yalnızca bir kez kuyruğa girer, tek düzey sınırı her iki yazma yolunda da tutar.<para>Depo sahtesi gerçek davranışı taklit eder: <c>GetByIdAsync</c> ve <c>GetAllAsync</c> küresel süzgeci uygular, <c>...ForAdmin</c> uygulamaz. Süzgeci sabitlemek yerine gerçekten çalıştırmak, "silinmiş yorum okura görünmez" gibi bir sözün testte gerçekten sınanmasının tek yoludur.</para></summary>
public class CommentServiceTests
{
    private static readonly DateTime Now = new(2026, 9, 7, 12, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IAdminNotifier> _adminNotifier = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IBlogRepository> _blogs = new();
    private readonly Mock<IRepository<Comment>> _comments = new();
    private readonly Mock<IRepository<CommentNotification>> _notifications = new();
    private readonly Mock<ITurnstileVerifier> _turnstile = new();

    private readonly List<Blog> _blogRows = [];
    private readonly List<Comment> _commentRows = [];
    private readonly List<CommentNotification> _notificationRows = [];

    private int _nextId = 100;
    private readonly CommentNotifySignal _signal = new();
    private readonly CommentService _sut;

    public CommentServiceTests()
    {
        _turnstile.Setup(t => t.VerifyAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _blogs.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => Live(_blogRows).FirstOrDefault(b => b.Id == id));
        _blogs.Setup(r => r.GetAllForAdminAsync(It.IsAny<Expression<Func<Blog, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Blog, bool>> p, CancellationToken _) => _blogRows.Where(p.Compile()).ToList());

        _comments.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => Live(_commentRows).FirstOrDefault(c => c.Id == id));
        _comments.Setup(r => r.GetByIdForAdminAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => _commentRows.FirstOrDefault(c => c.Id == id));
        _comments.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Comment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Comment, bool>> p, CancellationToken _) => Live(_commentRows).Where(p.Compile()).ToList());
        _comments.Setup(r => r.GetAllForAdminAsync(It.IsAny<Expression<Func<Comment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Comment, bool>> p, CancellationToken _) => _commentRows.Where(p.Compile()).ToList());
        _comments.Setup(r => r.GetAllPagedAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Expression<Func<Comment, bool>>?>(),
                It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int page, int size, Expression<Func<Comment, bool>>? p, bool _, CancellationToken __) =>
                Live(_commentRows).Where(c => p is null || p.Compile()(c))
                    .OrderBy(c => c.Id).Skip((page - 1) * size).Take(size).ToList());
        _comments.Setup(r => r.CountAsync(It.IsAny<Expression<Func<Comment, bool>>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Comment, bool>>? p, CancellationToken _) =>
                Live(_commentRows).Count(c => p is null || p.Compile()(c)));
        _comments.Setup(r => r.CountForAdminAsync(It.IsAny<Expression<Func<Comment, bool>>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Comment, bool>>? p, CancellationToken _) =>
                _commentRows.Count(c => p is null || p.Compile()(c)));
        _comments.Setup(r => r.AnyAsync(It.IsAny<Expression<Func<Comment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Comment, bool>> p, CancellationToken _) => Live(_commentRows).Any(p.Compile()));
        _comments.Setup(r => r.SelectForAdminPagedAsync(
                It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<Expression<Func<Comment, Comment>>>(),
                It.IsAny<Expression<Func<Comment, bool>>?>(),
                It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int _, int __, Expression<Func<Comment, Comment>> shape, Expression<Func<Comment, bool>>? p, bool ___, CancellationToken ____) =>
                _commentRows.Where(c => p is null || p.Compile()(c)).Select(shape.Compile()).ToList());
        _comments.Setup(r => r.AddAsync(It.IsAny<Comment>(), It.IsAny<CancellationToken>()))
            .Callback<Comment, CancellationToken>((c, _) => { c.Id = _nextId++; c.CreatedAt = Now; _commentRows.Add(c); })
            .Returns(Task.CompletedTask);
        _comments.Setup(r => r.UpdateAsync(It.IsAny<Comment>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _comments.Setup(r => r.SoftDeleteAsync(It.IsAny<Comment>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .Callback<Comment, int?, CancellationToken>((c, by, _) => { c.IsDeleted = true; c.IsActive = false; c.DeletedBy = by; })
            .Returns(Task.CompletedTask);

        _notifications.Setup(r => r.GetAllForAdminAsync(It.IsAny<Expression<Func<CommentNotification, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<CommentNotification, bool>> p, CancellationToken _) => _notificationRows.Where(p.Compile()).ToList());
        _notifications.Setup(r => r.AddAsync(It.IsAny<CommentNotification>(), It.IsAny<CancellationToken>()))
            .Callback<CommentNotification, CancellationToken>((n, _) => { n.Id = _nextId++; _notificationRows.Add(n); })
            .Returns(Task.CompletedTask);

        _uow.SetupGet(u => u.Blogs).Returns(_blogs.Object);
        _uow.SetupGet(u => u.Comments).Returns(_comments.Object);
        _uow.SetupGet(u => u.CommentNotifications).Returns(_notifications.Object);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var clock = Mock.Of<IClock>(c => c.UtcNow == Now);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Contact:ContactEmail"] = "yazar@site.test",
                ["Comments:AuthorName"] = "Furkan Tural"
            })
            .Build();

        _sut = new CommentService(
            _uow.Object, _turnstile.Object, config,
            new ActivityLogger(Mock.Of<ILogService>(), Mock.Of<IHttpContextAccessor>(), clock),
            _signal, NullLogger<CommentService>.Instance, _adminNotifier.Object, clock);
    }

    private static IEnumerable<T> Live<T>(IEnumerable<T> rows) where T : FurkanTural_Domain.Entities.Common.BaseEntity
        => rows.Where(r => !r.IsDeleted && r.IsActive);

    private Blog Post(int id = 7, string slug = "ornek")
    {
        var blog = new Blog { Id = id, Title = "Örnek yazı", Slug = slug };
        _blogRows.Add(blog);
        return blog;
    }

    private Comment Row(
        int id, int blogId = 7, int? parentId = null, string status = CommentStatuses.Approved,
        string email = "okur@site.test", bool notify = false, bool isDeleted = false, bool isActive = true, int? createdBy = null)
    {
        var row = new Comment
        {
            Id = id,
            BlogId = blogId,
            ParentId = parentId,
            AuthorName = "Okur " + id,
            AuthorEmail = email,
            Body = "Gövde " + id,
            Status = status,
            NotifyOnReply = notify,
            IsDeleted = isDeleted,
            IsActive = isActive,
            CreatedBy = createdBy,
            CreatedAt = Now.AddMinutes(-id)
        };
        _commentRows.Add(row);
        return row;
    }

    private static SubmitCommentDto Submission(int blogId = 7, int? parentId = null, string email = "yeni@site.test") => new()
    {
        BlogId = blogId,
        ParentId = parentId,
        AuthorName = "Yeni Okur",
        AuthorEmail = email,
        Body = "Yazı için teşekkürler."
    };

    [Fact]
    public void Okura_giden_yorum_adres_alani_tasimaz()
    {
        var names = typeof(CommentDto).GetProperties().Select(p => p.Name);

        names.Should().NotContain(n => n.Contains("Email", StringComparison.OrdinalIgnoreCase),
            "yorum listesi herkese açık bir uçtan gelir; adresi de taşısaydı sayfayı çeken biri yorumcuların adreslerini toplayabilirdi");
    }

    [Fact]
    public async Task Yeni_yorum_beklemede_acilir()
    {
        Post();

        var result = await _sut.SubmitAsync(Submission(), "jeton", "1.2.3.4");

        result.Success.Should().BeTrue();
        _commentRows.Should().ContainSingle().Which.Status.Should().Be(CommentStatuses.Pending,
            "onaydan önce görünen bir yorum, spam'i okurun karşısına koyardı");
    }

    [Fact]
    public async Task Bot_dogrulamasi_gecmeden_yorum_kaydedilmez()
    {
        Post();
        _turnstile.Setup(t => t.VerifyAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _sut.SubmitAsync(Submission(), null, null);

        result.Success.Should().BeFalse();
        _commentRows.Should().BeEmpty();
    }

    [Theory]
    [InlineData("bozuk-adres")]
    [InlineData("")]
    public async Task Gecersiz_adres_reddedilir(string email)
    {
        Post();

        var result = await _sut.SubmitAsync(Submission(email: email), "jeton", null);

        result.Success.Should().BeFalse();
        _commentRows.Should().BeEmpty();
    }

    [Fact]
    public async Task Adres_kucuk_harfe_indirilerek_saklanir()
    {
        Post();

        await _sut.SubmitAsync(Submission(email: "Okur@Site.Test"), "jeton", null);

        _commentRows.Should().ContainSingle().Which.AuthorEmail.Should().Be("okur@site.test",
            "aynı adresin iki farklı yazımı aynı kişiyi tanınmaz hâle getirirdi");
    }

    [Fact]
    public async Task Cok_kisa_govde_reddedilir()
    {
        Post();
        var dto = Submission();
        dto.Body = "a";

        var result = await _sut.SubmitAsync(dto, "jeton", null);

        result.Success.Should().BeFalse();
        _commentRows.Should().BeEmpty();
    }

    [Fact]
    public async Task Yayinda_olmayan_yaziya_yorum_birakilamaz()
    {
        var result = await _sut.SubmitAsync(Submission(blogId: 999), "jeton", null);

        result.StatusCode.Should().Be(404);
        _commentRows.Should().BeEmpty();
    }

    [Fact]
    public async Task Yanitin_yaniti_da_kabul_edilir()
    {
        Post();
        Row(1);
        Row(2, parentId: 1);

        var result = await _sut.SubmitAsync(Submission(parentId: 2), "jeton", null);

        result.Success.Should().BeTrue();
        _commentRows.Should().HaveCount(3);
        _commentRows[^1].ParentId.Should().Be(2,
            "zincirin derinliği veriyle değil sunumla sınırlanır");
    }

    [Fact]
    public async Task Onaylanmamis_yoruma_yanit_verilemez()
    {
        Post();
        Row(1, status: CommentStatuses.Pending);

        var result = await _sut.SubmitAsync(Submission(parentId: 1), "jeton", null);

        result.StatusCode.Should().Be(404, "bekleyen bir yorumun varlığını ele vermemek gerekir");
    }

    [Fact]
    public async Task Ayni_adres_pes_pese_iki_kayit_acamaz()
    {
        Post();
        Row(1, email: "okur@site.test", status: CommentStatuses.Pending).CreatedAt = Now.AddSeconds(-10);

        var result = await _sut.SubmitAsync(Submission(email: "okur@site.test"), "jeton", null);

        result.Success.Should().BeTrue("çift gönderime hata göstermek, kullanıcıyı olmayan bir sorunla uğraştırırdı");
        _commentRows.Should().HaveCount(1, "ikinci satır açılmamalı");
    }

    [Fact]
    public async Task Basarili_ve_yutulan_gonderim_ayni_metni_dondurur()
    {
        Post();
        var first = await _sut.SubmitAsync(Submission(email: "okur@site.test"), "jeton", null);
        var swallowed = await _sut.SubmitAsync(Submission(email: "okur@site.test"), "jeton", null);

        swallowed.Message.Should().Be(first.Message,
            "iki durumu ayıran bir metin, formu yazının hangi yorumlarının beklediğini sınayan bir araca çevirirdi");
    }

    [Fact]
    public async Task Okur_yalnizca_onayli_yorumlari_gorur()
    {
        Post();
        Row(1);
        Row(2, status: CommentStatuses.Pending);
        Row(3, status: CommentStatuses.Rejected);
        Row(4, isDeleted: true);
        Row(5, isActive: false);

        var result = await _sut.GetThreadAsync(7);

        result.Data!.Items.Should().ContainSingle().Which.Id.Should().Be(1);
        result.Data.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Yanitlar_koklerin_altina_yerlesir()
    {
        Post();
        Row(1);
        Row(2, parentId: 1);
        Row(3, parentId: 1);
        Row(4);

        var result = await _sut.GetThreadAsync(7);

        result.Data!.Items.Should().HaveCount(2, "yanıtlar kök sayılmaz");
        result.Data.Items[0].Replies.Select(r => r.Id).Should().Equal([2, 3]);
        result.Data.TotalCount.Should().Be(4, "başlıktaki sayı yanıtları da sayar");
    }

    [Fact]
    public async Task Zincir_kac_seviye_inerse_insin_agaca_yerlesir()
    {
        Post();
        Row(1);
        Row(2, parentId: 1);
        Row(3, parentId: 2);
        Row(4, parentId: 3);

        var result = await _sut.GetThreadAsync(7);

        var kok = result.Data!.Items.Should().ContainSingle().Subject;
        var birinci = kok.Replies.Should().ContainSingle().Subject;
        var ikinci = birinci.Replies.Should().ContainSingle().Subject;
        ikinci.Replies.Should().ContainSingle().Which.Id.Should().Be(4);
        result.Data.TotalCount.Should().Be(4, "başlıktaki sayı zincirin tamamını sayar");
    }

    [Fact]
    public async Task Yayindan_kalkan_yorumun_yaniti_koke_terfi_etmez()
    {
        Post();
        Row(1);
        Row(2, parentId: 1, status: CommentStatuses.Pending);
        Row(3, parentId: 2);

        var result = await _sut.GetThreadAsync(7);

        var kok = result.Data!.Items.Should().ContainSingle().Subject;
        kok.Replies.Should().BeEmpty("okurun göremediği bir yoruma verilen yanıt bağlamsız kalırdı");
    }

    [Fact]
    public async Task Yazarin_yaniti_isaretlenir()
    {
        Post();
        Row(1);
        Row(2, parentId: 1, createdBy: 9);

        var result = await _sut.GetThreadAsync(7);

        result.Data!.Items[0].Replies.Should().ContainSingle().Which.IsAuthor.Should().BeTrue();
        result.Data.Items[0].IsAuthor.Should().BeFalse();
    }

    [Fact]
    public async Task Gecersiz_durum_kabul_edilmez()
    {
        Post();
        Row(1, status: CommentStatuses.Pending);

        var result = await _sut.SetStatusAsync(1, "Yayinda", null);

        result.Success.Should().BeFalse();
        _commentRows[0].Status.Should().Be(CommentStatuses.Pending);
    }

    [Fact]
    public async Task Silinmis_yorumun_durumu_degistirilemez()
    {
        Post();
        Row(1, status: CommentStatuses.Pending, isDeleted: true);

        var result = await _sut.SetStatusAsync(1, CommentStatuses.Approved, null);

        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Onay_tarihi_bir_kez_damgalanir()
    {
        Post();
        var row = Row(1, status: CommentStatuses.Pending);

        await _sut.SetStatusAsync(1, CommentStatuses.Approved, null);
        var first = row.ApprovedAt;
        await _sut.SetStatusAsync(1, CommentStatuses.Rejected, null);
        await _sut.SetStatusAsync(1, CommentStatuses.Approved, null);

        row.ApprovedAt.Should().Be(first, "ilk yayın anı sonraki kararlarla kaymamalı");
    }

    [Fact]
    public async Task Onaylanan_yanit_bildirim_kuyruguna_girer()
    {
        Post();
        Row(1, email: "ust@site.test", notify: true);
        Row(2, parentId: 1, status: CommentStatuses.Pending, email: "yanit@site.test");

        await _sut.SetStatusAsync(2, CommentStatuses.Approved, null);

        _notificationRows.Should().ContainSingle();
        _notificationRows[0].CommentId.Should().Be(2);
        _notificationRows[0].Email.Should().Be("ust@site.test");
        _notificationRows[0].Status.Should().Be(CommentNotificationStatuses.Pending);
    }

    [Fact]
    public async Task Bildirim_istemeyen_uste_kuyruk_acilmaz()
    {
        Post();
        Row(1, email: "ust@site.test", notify: false);
        Row(2, parentId: 1, status: CommentStatuses.Pending, email: "yanit@site.test");

        await _sut.SetStatusAsync(2, CommentStatuses.Approved, null);

        _notificationRows.Should().BeEmpty();
    }

    [Fact]
    public async Task Kisi_kendi_yorumunun_yanitindan_haberdar_edilmez()
    {
        Post();
        Row(1, email: "ayni@site.test", notify: true);
        Row(2, parentId: 1, status: CommentStatuses.Pending, email: "ayni@site.test");

        await _sut.SetStatusAsync(2, CommentStatuses.Approved, null);

        _notificationRows.Should().BeEmpty("kişi zaten kendi yazdığını biliyor");
    }

    [Fact]
    public async Task Reddedilip_yeniden_onaylanan_yanit_ikinci_posta_uretmez()
    {
        Post();
        Row(1, email: "ust@site.test", notify: true);
        Row(2, parentId: 1, status: CommentStatuses.Pending, email: "yanit@site.test");

        await _sut.SetStatusAsync(2, CommentStatuses.Approved, null);
        await _sut.SetStatusAsync(2, CommentStatuses.Rejected, null);
        await _sut.SetStatusAsync(2, CommentStatuses.Approved, null);

        _notificationRows.Should().ContainSingle("okur için o iki onay tek bir olaydır");
    }

    [Fact]
    public async Task Kok_yorumun_onayi_bildirim_uretmez()
    {
        Post();
        Row(1, status: CommentStatuses.Pending, notify: true);

        await _sut.SetStatusAsync(1, CommentStatuses.Approved, null);

        _notificationRows.Should().BeEmpty("bildirilecek bir üst yorum yok");
    }

    [Fact]
    public async Task Panelden_verilen_yanit_beklemeden_yayina_girer()
    {
        Post();
        Row(1, email: "ust@site.test", notify: true);

        var result = await _sut.ReplyAsync(new AdminReplyCommentDto { ParentId = 1, Body = "Teşekkürler." }, 9);

        result.Success.Should().BeTrue();
        var reply = _commentRows.Single(c => c.Id != 1);
        reply.Status.Should().Be(CommentStatuses.Approved);
        reply.ParentId.Should().Be(1);
        reply.CreatedBy.Should().Be(9, "yazarlık damgası oluşturan kimliğinden okunur");
        reply.AuthorEmail.Should().Be("yazar@site.test");
        reply.AuthorName.Should().Be("Furkan Tural");
        _notificationRows.Should().ContainSingle();
    }

    [Fact]
    public async Task Panelden_yanita_da_yanit_verilebilir()
    {
        Post();
        Row(1);
        Row(2, parentId: 1);

        var result = await _sut.ReplyAsync(new AdminReplyCommentDto { ParentId = 2, Body = "Devam edelim." }, 9);

        result.Success.Should().BeTrue();
        _commentRows.Should().HaveCount(3);
        _commentRows[^1].ParentId.Should().Be(2,
            "yazarın bir yanıta karşılık verememesi, konuşmayı okurun başlattığı yerde bırakırdı");
    }

    [Fact]
    public async Task Panelden_onaysiz_yoruma_yanit_verilemez()
    {
        Post();
        Row(1, status: CommentStatuses.Pending);

        var result = await _sut.ReplyAsync(new AdminReplyCommentDto { ParentId = 1, Body = "Olmaz." }, 9);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Contain("onaylanmış");
    }

    [Fact]
    public async Task Bos_yanit_kabul_edilmez()
    {
        Post();
        Row(1);

        var result = await _sut.ReplyAsync(new AdminReplyCommentDto { ParentId = 1, Body = "  " }, 9);

        result.Success.Should().BeFalse();
        _commentRows.Should().HaveCount(1);
    }

    [Fact]
    public async Task Gecersiz_jeton_bildirimleri_kapatmaz()
    {
        Post();
        Row(1, email: "okur@site.test", notify: true);

        var result = await _sut.DisableNotificationsAsync("uydurma");

        result.Success.Should().BeFalse();
        _commentRows[0].NotifyOnReply.Should().BeTrue();
    }

    [Fact]
    public async Task Cikis_jetonu_adresin_butun_yorumlarini_kapatir()
    {
        Post();
        Row(1, email: "okur@site.test", notify: true);
        Row(2, email: "okur@site.test", notify: true);
        Row(3, email: "baska@site.test", notify: true);
        _notificationRows.Add(new CommentNotification
        {
            Id = 50,
            CommentId = 1,
            Email = "okur@site.test",
            Status = CommentNotificationStatuses.Sent,
            TokenHash = Hash("acik-jeton")
        });

        var result = await _sut.DisableNotificationsAsync("acik-jeton");

        result.Success.Should().BeTrue();
        _commentRows.Single(c => c.Id == 1).NotifyOnReply.Should().BeFalse();
        _commentRows.Single(c => c.Id == 2).NotifyOnReply.Should().BeFalse(
            "\"bana posta göndermeyi bırak\" diyen biri yalnızca o tek yorum için söylemiyordur");
        _commentRows.Single(c => c.Id == 3).NotifyOnReply.Should().BeTrue("başkasının tercihine dokunulmaz");
    }

    [Fact]
    public async Task Denetim_sayaclari_silinmisleri_saymaz()
    {
        Post();
        Row(1, status: CommentStatuses.Pending);
        Row(2, status: CommentStatuses.Pending, isDeleted: true);
        Row(3);
        Row(4, status: CommentStatuses.Rejected);

        var result = await _sut.GetModerationCountsAsync();

        result.Data!.Pending.Should().Be(1);
        result.Data.Approved.Should().Be(1);
        result.Data.Rejected.Should().Be(1);
    }

    [Fact]
    public async Task Yonetim_listesi_adres_ve_yazi_basligini_tasir()
    {
        Post();
        Row(1, status: CommentStatuses.Pending, email: "okur@site.test");
        _comments.Setup(r => r.GetAllForAdminPagedAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Expression<Func<Comment, bool>>?>(),
                It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => _commentRows.ToList());

        var result = await _sut.GetAllForAdminPagedAsync(new AdminListQuery(), null, null);

        var row = result.Data!.Single();
        row.AuthorEmail.Should().Be("okur@site.test", "denetim kararı adresi görmeden verilemez");
        row.BlogTitle.Should().Be("Örnek yazı");
    }

    [Fact]
    public async Task Yanit_sayisi_tek_okumada_toplanir()
    {
        Post();
        Row(1);
        Row(2, parentId: 1);
        Row(3, parentId: 1);
        _comments.Setup(r => r.GetAllForAdminPagedAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Expression<Func<Comment, bool>>?>(),
                It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => _commentRows.ToList());

        var result = await _sut.GetAllForAdminPagedAsync(new AdminListQuery(), null, null);

        result.Data!.Single(c => c.Id == 1).ReplyCount.Should().Be(2);
        _comments.Verify(r => r.SelectForAdminPagedAsync(
            It.IsAny<int>(), It.IsAny<int>(),
            It.IsAny<Expression<Func<Comment, Comment>>>(),
            It.IsAny<Expression<Func<Comment, bool>>?>(),
            It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once,
            "yorum başına ayrı sayım, listenin kendisi kadar sorgu açardı");
    }

    private static string Hash(string token)
        => Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));

    /// <summary>Yeni yorum, açık duran yönetici paneline haber gitmesini gerektirir. Bildirim kaydın yerine geçmez — düşerse sayı bir sonraki sayfa yüklemesinde düzelir — ama hiç gönderilmezse sekmesini açık bırakan yönetici kuyruğun boş olduğunu sanır.</summary>
    [Fact]
    public async Task Yeni_yorum_yoneticiye_haber_verir()
    {
        Post();

        await _sut.SubmitAsync(Submission(), "jeton", null);

        _adminNotifier.Verify(
            n => n.NotifyPendingWorkChangedAsync(AdminWorkKinds.Comment, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>Cooldown dalı kayıt açmaz, dolayısıyla haber verecek bir şey de yoktur. Buradan bildirim çıkarsa panelde olmayan bir iş görünür ve yönetici boş kuyruğa bakmaya gider.</summary>
    [Fact]
    public async Task Yutulan_gonderim_yoneticiye_haber_vermez()
    {
        Post();
        Row(1, email: "okur@site.test", status: CommentStatuses.Pending).CreatedAt = Now.AddSeconds(-10);

        await _sut.SubmitAsync(Submission(email: "okur@site.test"), "jeton", null);

        _adminNotifier.Verify(
            n => n.NotifyPendingWorkChangedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>Doğrulamada elenen gönderim de kayıt açmaz; bildirim yalnızca gerçekten satır açıldığında çıkmalı.</summary>
    [Fact]
    public async Task Bot_dogrulamasi_gecilemezse_haber_gitmez()
    {
        Post();
        _turnstile.Setup(t => t.VerifyAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await _sut.SubmitAsync(Submission(), null, null);

        _adminNotifier.Verify(
            n => n.NotifyPendingWorkChangedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
