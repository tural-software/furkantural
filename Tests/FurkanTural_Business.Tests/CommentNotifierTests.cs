using System.Linq.Expressions;
using FluentAssertions;
using FurkanTural_Application.DTOs.Mail;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using FurkanTural_Business.Services.Concrete;
using FurkanTural_Domain.Constants;
using FurkanTural_Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FurkanTural_Business.Tests;

/// <summary>Bildirim turunun üç sözü sınanır: kimse aynı bildirimi iki kez almaz, geçici arıza deneme haklarını tek turda yakmaz ve kuyruğa girdikten sonra değişen koşullar gönderimi durdurur.<para>Çıkış jetonunun özeti <b>yalnızca gönderim başarılıysa</b> yazılır; ters sıra, gönderilemeyen her denemede kullanılmayacak bir kimlik bilgisi biriktirirdi.</para></summary>
public class CommentNotifierTests
{
    private const string UnsubscribeUrl = "https://blog.test/yorum/bildirim-kapat";
    private const string PostUrl = "https://blog.test/yazi/{slug}";

    private static readonly DateTime Now = new(2026, 9, 7, 12, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IBlogRepository> _blogs = new();
    private readonly Mock<IRepository<Comment>> _comments = new();
    private readonly Mock<IRepository<CommentNotification>> _notifications = new();
    private readonly Mock<IMailSender> _mail = new();

    private readonly List<Blog> _blogRows = [];
    private readonly List<Comment> _commentRows = [];
    private readonly List<CommentNotification> _notificationRows = [];
    private readonly List<(string? To, CommentReplyMailDto Payload)> _sent = [];

    private Func<string?, Result> _sendOutcome = _ => Result.Ok();

    private readonly Dictionary<string, string?> _settings = new()
    {
        ["Comments:UnsubscribeUrl"] = UnsubscribeUrl,
        ["Comments:PostUrl"] = PostUrl,
        ["Contact:ContactEmail"] = "yazar@site.test"
    };

    public CommentNotifierTests()
    {
        _blogs.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) =>
                _blogRows.FirstOrDefault(b => b.Id == id && !b.IsDeleted && b.IsActive));

        _comments.Setup(r => r.GetByIdForAdminAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => _commentRows.FirstOrDefault(c => c.Id == id));

        _notifications.Setup(r => r.GetAllPagedAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Expression<Func<CommentNotification, bool>>?>(),
                It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int page, int size, Expression<Func<CommentNotification, bool>>? p, bool _, CancellationToken __) =>
                _notificationRows.Where(n => !n.IsDeleted && n.IsActive)
                    .Where(n => p is null || p.Compile()(n))
                    .OrderBy(n => n.Id).Skip((page - 1) * size).Take(size).ToList());
        _notifications.Setup(r => r.UpdateAsync(It.IsAny<CommentNotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mail.Setup(m => m.SendAsync(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, string? __, string? to, object payload, CancellationToken ___) =>
            {
                _sent.Add((to, (CommentReplyMailDto)payload));
                return _sendOutcome(to);
            });

        _uow.SetupGet(u => u.Blogs).Returns(_blogs.Object);
        _uow.SetupGet(u => u.Comments).Returns(_comments.Object);
        _uow.SetupGet(u => u.CommentNotifications).Returns(_notifications.Object);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private CommentNotifier Build()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(_settings).Build();
        return new CommentNotifier(
            _uow.Object, _mail.Object, config, NullLogger<CommentNotifier>.Instance,
            Mock.Of<IClock>(c => c.UtcNow == Now));
    }

    private void Scene(bool parentNotify = true, string replyStatus = CommentStatuses.Approved, bool parentDeleted = false, bool blogActive = true)
    {
        _blogRows.Add(new Blog { Id = 7, Title = "Örnek yazı", Slug = "ornek-yazi", IsActive = blogActive });
        _commentRows.Add(new Comment
        {
            Id = 1,
            BlogId = 7,
            AuthorName = "Okur",
            AuthorEmail = "ust@site.test",
            Body = "İlk yorum",
            Status = CommentStatuses.Approved,
            NotifyOnReply = parentNotify,
            IsDeleted = parentDeleted,
            IsActive = !parentDeleted
        });
        _commentRows.Add(new Comment
        {
            Id = 2,
            BlogId = 7,
            ParentId = 1,
            AuthorName = "Yanıtlayan",
            AuthorEmail = "yanit@site.test",
            Body = "Katılıyorum",
            Status = replyStatus,
            CreatedAt = Now.AddMinutes(-5)
        });
        _notificationRows.Add(new CommentNotification
        {
            Id = 10,
            CommentId = 2,
            Email = "ust@site.test",
            Status = CommentNotificationStatuses.Pending
        });
    }

    [Fact]
    public async Task Yapilandirma_eksikse_hicbir_posta_gitmez()
    {
        Scene();
        _settings["Comments:UnsubscribeUrl"] = null;

        var processed = await Build().NotifyAsync();

        processed.Should().Be(0);
        _sent.Should().BeEmpty("çalışmayan bir kapatma bağlantısı taşıyan bildirim göndermek, adresi izinsiz posta almaya mahkûm ederdi");
    }

    [Fact]
    public async Task Yazi_adresi_yer_tutucusuz_ise_gonderim_yapilmaz()
    {
        Scene();
        _settings["Comments:PostUrl"] = "https://blog.test/yazi";

        var processed = await Build().NotifyAsync();

        processed.Should().Be(0);
        _sent.Should().BeEmpty();
    }

    [Fact]
    public async Task Bildirim_ust_yorumun_sahibine_gider()
    {
        Scene();

        await Build().NotifyAsync();

        _sent.Should().ContainSingle();
        _sent[0].To.Should().Be("ust@site.test");
        _sent[0].Payload.RecipientName.Should().Be("Okur");
        _sent[0].Payload.ReplyAuthorName.Should().Be("Yanıtlayan");
        _sent[0].Payload.PostTitle.Should().Be("Örnek yazı");
        _sent[0].Payload.PostUrl.Should().Be("https://blog.test/yazi/ornek-yazi#yorum-2",
            "bağlantı yanıtın kendisine inmeli; yazının başına indirmek okuru aramaya bırakırdı");
        _sent[0].Payload.UnsubscribeUrl.Should().StartWith(UnsubscribeUrl + "?token=");
    }

    [Fact]
    public async Task Basarili_gonderimde_jeton_ozeti_yazilir()
    {
        Scene();

        await Build().NotifyAsync();

        var row = _notificationRows[0];
        row.Status.Should().Be(CommentNotificationStatuses.Sent);
        row.SentAt.Should().Be(Now);
        row.TokenHash.Should().NotBeNullOrWhiteSpace();
        _sent[0].Payload.UnsubscribeUrl.Should().NotContain(row.TokenHash!,
            "jetonun düz hâli postada, özeti tabloda durur; ikisi aynı değer olsaydı özetin bir anlamı kalmazdı");
    }

    [Fact]
    public async Task Basarisiz_gonderim_jeton_birakmaz()
    {
        Scene();
        _sendOutcome = _ => Result.Fail("SMTP kapalı");

        await Build().NotifyAsync();

        var row = _notificationRows[0];
        row.TokenHash.Should().BeNull("gönderilemeyen bir postanın jetonu veri tabanında iz bırakmamalı");
        row.Status.Should().Be(CommentNotificationStatuses.Pending);
        row.AttemptCount.Should().Be(1);
    }

    [Fact]
    public async Task Deneme_haklari_tek_turda_tukenmez()
    {
        Scene();
        _sendOutcome = _ => Result.Fail("SMTP kapalı");
        var notifier = Build();

        await notifier.NotifyAsync();
        await notifier.NotifyAsync();

        _notificationRows[0].AttemptCount.Should().Be(2, "bir tur her satırı bir kez dener ve aynı satıra o tur içinde dönmez");
        _notificationRows[0].Status.Should().Be(CommentNotificationStatuses.Pending);
    }

    [Fact]
    public async Task Deneme_hakki_bitince_satir_basarisiz_isaretlenir()
    {
        Scene();
        _sendOutcome = _ => Result.Fail("SMTP kapalı");
        var notifier = Build();

        for (var i = 0; i < CommentNotifier.MaxAttempts; i++)
            await notifier.NotifyAsync();

        _notificationRows[0].Status.Should().Be(CommentNotificationStatuses.Failed);
        _notificationRows[0].Error.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Gonderilen_satir_ikinci_turda_tekrar_denenmez()
    {
        Scene();
        var notifier = Build();

        await notifier.NotifyAsync();
        await notifier.NotifyAsync();

        _sent.Should().ContainSingle("kimse aynı bildirimi iki kez almamalı");
    }

    [Fact]
    public async Task Kuyruktan_sonra_kapatilan_bildirim_atlanir()
    {
        Scene(parentNotify: false);

        await Build().NotifyAsync();

        _sent.Should().BeEmpty();
        _notificationRows[0].Status.Should().Be(CommentNotificationStatuses.Skipped,
            "başarısızlık değil; alıcının kararına uymak doğru davranıştır");
    }

    [Fact]
    public async Task Yayindan_kaldirilan_yanit_duyurulmaz()
    {
        Scene(replyStatus: CommentStatuses.Rejected);

        await Build().NotifyAsync();

        _sent.Should().BeEmpty();
        _notificationRows[0].Status.Should().Be(CommentNotificationStatuses.Skipped);
    }

    [Fact]
    public async Task Silinen_ust_yoruma_bildirim_gitmez()
    {
        Scene(parentDeleted: true);

        await Build().NotifyAsync();

        _sent.Should().BeEmpty();
        _notificationRows[0].Status.Should().Be(CommentNotificationStatuses.Skipped);
    }

    [Fact]
    public async Task Yayindan_kalkan_yaziya_cagiran_bildirim_gonderilmez()
    {
        Scene(blogActive: false);

        await Build().NotifyAsync();

        _sent.Should().BeEmpty();
        _notificationRows[0].Status.Should().Be(CommentNotificationStatuses.Skipped,
            "okuru 404'e götüren bir bildirim göndermek, hiç göndermemekten kötüdür");
    }
}
