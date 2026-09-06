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

/// <summary>Dağıtıcının üç sözü sınanır: kimse postayı iki kez almaz, geçici arıza deneme haklarını tek turda yakmaz ve liste dondurulduktan sonra çıkanlara gönderilmez.<para>Depo sahtesi gerçek davranışı taklit eder: sorgular derlenip bellekteki satırlara uygulanır, kimliğe göre sıralanır ve sayfalanır. Sayfalamayı sabitlemek yerine gerçekten çalıştırmak, imleç mantığındaki bir hatanın teste yakalanmasının tek yoludur.</para></summary>
public class NewsletterDispatcherTests
{
    private const string UnsubscribeUrl = "https://blog.test/bulten/cikis";

    private static readonly DateTime Now = new(2026, 9, 7, 10, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ISubscriberRepository> _subscribers = new();
    private readonly Mock<IRepository<NewsletterIssue>> _issues = new();
    private readonly Mock<IRepository<NewsletterDelivery>> _deliveries = new();
    private readonly Mock<IRepository<SubscriberVerification>> _verifications = new();
    private readonly Mock<IMailSender> _mail = new();

    private readonly List<NewsletterIssue> _issueRows = [];
    private readonly List<NewsletterDelivery> _deliveryRows = [];
    private readonly List<Subscriber> _subscriberRows = [];
    private readonly List<SubscriberVerification> _issuedTokens = [];
    private readonly List<(string? To, object Payload)> _sent = [];

    private Func<string?, Result> _sendOutcome = _ => Result.Ok();

    private readonly NewsletterDispatcher _sut;

    public NewsletterDispatcherTests()
    {
        _issues.Setup(r => r.GetAllPagedAsync(
                It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<Expression<Func<NewsletterIssue, bool>>?>(),
                It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int page, int size, Expression<Func<NewsletterIssue, bool>>? p, bool _, CancellationToken __) =>
                Live(_issueRows).Where(i => p is null || p.Compile()(i))
                    .OrderBy(i => i.Id).Skip((page - 1) * size).Take(size).ToList());
        _issues.Setup(r => r.UpdateAsync(It.IsAny<NewsletterIssue>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _deliveries.Setup(r => r.GetAllPagedAsync(
                It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<Expression<Func<NewsletterDelivery, bool>>?>(),
                It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int page, int size, Expression<Func<NewsletterDelivery, bool>>? p, bool _, CancellationToken __) =>
                Live(_deliveryRows).Where(d => p is null || p.Compile()(d))
                    .OrderBy(d => d.Id).Skip((page - 1) * size).Take(size).ToList());
        _deliveries.Setup(r => r.CountAsync(It.IsAny<Expression<Func<NewsletterDelivery, bool>>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<NewsletterDelivery, bool>>? p, CancellationToken _) =>
                Live(_deliveryRows).Count(d => p is null || p.Compile()(d)));
        _deliveries.Setup(r => r.UpdateAsync(It.IsAny<NewsletterDelivery>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _subscribers.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Subscriber, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Subscriber, bool>> p, CancellationToken _) =>
                Live(_subscriberRows).Where(p.Compile()).ToList());

        _verifications.Setup(r => r.AddAsync(It.IsAny<SubscriberVerification>(), It.IsAny<CancellationToken>()))
            .Callback<SubscriberVerification, CancellationToken>((v, _) => _issuedTokens.Add(v))
            .Returns(Task.CompletedTask);

        _mail.Setup(m => m.SendAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, string? __, string? to, object payload, CancellationToken ___) =>
            {
                var outcome = _sendOutcome(to);
                if (outcome.Success) _sent.Add((to, payload));
                return outcome;
            });

        _uow.SetupGet(u => u.Subscribers).Returns(_subscribers.Object);
        _uow.SetupGet(u => u.NewsletterIssues).Returns(_issues.Object);
        _uow.SetupGet(u => u.NewsletterDeliveries).Returns(_deliveries.Object);
        _uow.SetupGet(u => u.SubscriberVerifications).Returns(_verifications.Object);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Newsletter:UnsubscribeUrl"] = UnsubscribeUrl
        }).Build();

        _sut = new NewsletterDispatcher(
            _uow.Object, _mail.Object, config,
            NullLogger<NewsletterDispatcher>.Instance,
            Mock.Of<IClock>(c => c.UtcNow == Now));
    }

    /// <summary>Küresel süzgecin karşılığı; dağıtıcının bütün okumaları bundan geçer.</summary>
    private static IEnumerable<T> Live<T>(IEnumerable<T> rows) where T : FurkanTural_Domain.Entities.Common.BaseEntity
        => rows.Where(r => !r.IsDeleted && r.IsActive);

    private NewsletterIssue Sending(int recipients)
    {
        var issue = new NewsletterIssue
        {
            Id = 3,
            Subject = "Eylül notları",
            Body = "<p>Merhaba</p>",
            Status = NewsletterIssueStatuses.Sending,
            QueuedAt = Now.AddMinutes(-1),
            RecipientCount = recipients
        };
        _issueRows.Add(issue);

        for (var i = 1; i <= recipients; i++)
        {
            _subscriberRows.Add(new Subscriber { Id = i, Email = $"okur{i}@ornek.test", ConfirmedAt = Now.AddDays(-1) });
            _deliveryRows.Add(new NewsletterDelivery
            {
                Id = i,
                NewsletterIssueId = 3,
                SubscriberId = i,
                Email = $"okur{i}@ornek.test",
                Status = NewsletterDeliveryStatuses.Pending
            });
        }

        return issue;
    }

    [Fact]
    public async Task Bekleyen_her_alici_bir_kez_alir_ve_sayi_kapanir()
    {
        var issue = Sending(3);

        var processed = await _sut.DispatchAsync();

        processed.Should().Be(3);
        _sent.Should().HaveCount(3);
        _sent.Select(s => s.To).Should().OnlyHaveUniqueItems();
        _deliveryRows.Should().OnlyContain(d => d.Status == NewsletterDeliveryStatuses.Sent && d.SentAt == Now);
        issue.Status.Should().Be(NewsletterIssueStatuses.Sent);
        issue.CompletedAt.Should().Be(Now);
        issue.SentCount.Should().Be(3);
    }

    [Fact]
    public async Task Ikinci_tur_gonderilmis_aliciya_tekrar_gitmez()
    {
        Sending(2);

        await _sut.DispatchAsync();
        var second = await _sut.DispatchAsync();

        second.Should().Be(0, "gönderilmiş satır bekleyen değildir");
        _sent.Should().HaveCount(2);
    }

    [Fact]
    public async Task Sayfa_boyutunu_asan_liste_tek_turda_bitirilir()
    {
        var total = NewsletterDispatcher.BatchSize * 2 + 7;
        var issue = Sending(total);

        var processed = await _sut.DispatchAsync();

        processed.Should().Be(total, "imleç sayfalar arasında satır atlamamalı");
        _sent.Should().HaveCount(total);
        issue.Status.Should().Be(NewsletterIssueStatuses.Sent);
    }

    [Fact]
    public async Task Gecici_ariza_deneme_haklarini_tek_turda_yakmaz()
    {
        var issue = Sending(2);
        _sendOutcome = _ => Result.Fail("Posta gönderilemedi.", "SMTP kapalı", 502);

        await _sut.DispatchAsync();

        _deliveryRows.Should().OnlyContain(d => d.AttemptCount == 1 && d.Status == NewsletterDeliveryStatuses.Pending);
        issue.Status.Should().Be(NewsletterIssueStatuses.Sending, "bekleyen satır varken sayı kapanmamalı");
    }

    [Fact]
    public async Task Deneme_hakki_tukenince_satir_kalici_olarak_basarisiz_olur()
    {
        var issue = Sending(1);
        _sendOutcome = _ => Result.Fail("Posta gönderilemedi.", "SMTP kapalı", 502);

        for (var i = 0; i < NewsletterDispatcher.MaxAttempts; i++)
            await _sut.DispatchAsync();

        _deliveryRows[0].Status.Should().Be(NewsletterDeliveryStatuses.Failed);
        _deliveryRows[0].AttemptCount.Should().Be(NewsletterDispatcher.MaxAttempts);
        _deliveryRows[0].Error.Should().Contain("SMTP kapalı");
        issue.Status.Should().Be(NewsletterIssueStatuses.Sent, "bekleyen kalmadıysa sayı kapanır");
        issue.FailedCount.Should().Be(1);
    }

    [Fact]
    public async Task Basarisiz_gonderim_jeton_birakmaz()
    {
        Sending(2);
        _sendOutcome = to => to == "okur1@ornek.test" ? Result.Ok() : Result.Fail("Posta gönderilemedi.", "SMTP", 502);

        await _sut.DispatchAsync();

        _issuedTokens.Should().ContainSingle("yalnızca gerçekten giden postanın jetonu kaydedilmeli");
        _issuedTokens[0].SubscriberId.Should().Be(1);
        _issuedTokens[0].Purpose.Should().Be(SubscriberVerificationPurposes.Unsubscribe);
    }

    [Fact]
    public async Task Cikis_jetonu_bulten_omru_boyunca_gecerlidir()
    {
        Sending(1);

        await _sut.DispatchAsync();

        _issuedTokens[0].ExpiresAt.Should().Be(Now.Add(NewsletterDispatcher.UnsubscribeLifetime));
        _issuedTokens[0].TokenHash.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Her_alicinin_cikis_baglantisi_kendine_ozeldir()
    {
        Sending(3);

        await _sut.DispatchAsync();

        var links = _sent.Select(s => ((NewsletterIssueMailDto)s.Payload).UnsubscribeUrl).ToList();
        links.Should().OnlyHaveUniqueItems();
        links.Should().OnlyContain(l => l!.StartsWith(UnsubscribeUrl + "?token="));
    }

    [Fact]
    public async Task Dondurmadan_sonra_cikan_aliciya_gonderilmez()
    {
        var issue = Sending(2);
        _subscriberRows[1].IsDeleted = true;

        await _sut.DispatchAsync();

        _sent.Should().ContainSingle();
        _deliveryRows[1].Status.Should().Be(NewsletterDeliveryStatuses.Skipped);
        _deliveryRows[1].Error.Should().Contain("abonelik sona erdi");
        issue.SkippedCount.Should().Be(1);
        issue.Status.Should().Be(NewsletterIssueStatuses.Sent);
    }

    [Fact]
    public async Task Dogrulamasi_geri_alinmis_aliciya_gonderilmez()
    {
        Sending(1);
        _subscriberRows[0].ConfirmedAt = null;

        await _sut.DispatchAsync();

        _sent.Should().BeEmpty();
        _deliveryRows[0].Status.Should().Be(NewsletterDeliveryStatuses.Skipped);
    }

    [Fact]
    public async Task Pasife_alinmis_sayi_dagitilmaz()
    {
        var issue = Sending(2);
        issue.IsActive = false;

        var processed = await _sut.DispatchAsync();

        processed.Should().Be(0);
        _sent.Should().BeEmpty();
        _deliveryRows.Should().OnlyContain(d => d.Status == NewsletterDeliveryStatuses.Pending, "duraklatma satırları kaybetmemeli");
    }

    [Fact]
    public async Task Duraklatilan_dagitim_kaldigi_yerden_surer()
    {
        var issue = Sending(4);
        _deliveryRows[0].Status = NewsletterDeliveryStatuses.Sent;
        _deliveryRows[1].Status = NewsletterDeliveryStatuses.Sent;

        await _sut.DispatchAsync();

        _sent.Select(s => s.To).Should().BeEquivalentTo("okur3@ornek.test", "okur4@ornek.test");
        issue.SentCount.Should().Be(4);
    }

    [Fact]
    public async Task Cikis_adresi_yoksa_hicbir_posta_gitmez()
    {
        Sending(2);
        var dispatcher = new NewsletterDispatcher(
            _uow.Object, _mail.Object, new ConfigurationBuilder().Build(),
            NullLogger<NewsletterDispatcher>.Instance,
            Mock.Of<IClock>(c => c.UtcNow == Now));

        var processed = await dispatcher.DispatchAsync();

        processed.Should().Be(0);
        _sent.Should().BeEmpty("çıkış bağlantısı üretilemeyen bülten izinsiz listeye dönerdi");
    }

    [Fact]
    public async Task Taslak_sayi_dagitima_girmez()
    {
        var issue = Sending(2);
        issue.Status = NewsletterIssueStatuses.Draft;

        var processed = await _sut.DispatchAsync();

        processed.Should().Be(0);
        _sent.Should().BeEmpty();
    }

    [Fact]
    public async Task Gonderilen_postanin_govdesi_sayinin_govdesidir()
    {
        Sending(1);

        await _sut.DispatchAsync();

        var payload = (NewsletterIssueMailDto)_sent[0].Payload;
        payload.Subject.Should().Be("Eylül notları");
        payload.Body.Should().Be("<p>Merhaba</p>");
        payload.Email.Should().Be("okur1@ornek.test");
    }
}
