using System.Linq.Expressions;
using FluentAssertions;
using FurkanTural_Application.DTOs.Mail;
using FurkanTural_Application.DTOs.Newsletter;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using FurkanTural_Business.Helpers;
using FurkanTural_Business.Services.Concrete;
using FurkanTural_Domain.Constants;
using FurkanTural_Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;

namespace FurkanTural_Business.Tests;

/// <summary>Bültenin yazımı ve dağıtıma verilmesi. Turun tek geri alınamaz adımı dondurmadır; testlerin ağırlığı bu yüzden <see cref="NewsletterIssueService.QueueAsync"/> çevresinde toplanır — yanlış kurulmuş bir kuyruk, yanlış kişilere gitmiş posta demektir.</summary>
public class NewsletterIssueServiceTests
{
    private const string UnsubscribeUrl = "https://blog.test/bulten/cikis";

    private static readonly DateTime Now = new(2026, 9, 7, 10, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ISubscriberRepository> _subscribers = new();
    private readonly Mock<IRepository<NewsletterIssue>> _issues = new();
    private readonly Mock<IRepository<NewsletterDelivery>> _deliveries = new();
    private readonly Mock<IRepository<MailTemplateType>> _types = new();
    private readonly Mock<IRepository<MailTemplate>> _templates = new();
    private readonly Mock<IMailSender> _mail = new();

    private readonly List<Subscriber> _audience = [];
    private readonly List<NewsletterDelivery> _queued = [];
    private readonly List<(string Type, string? To, object Payload)> _sent = [];
    private readonly ActivityJournal _journal = new();

    private Result _outcome = Result.Ok();
    private NewsletterIssue? _issue;
    private bool _templateExists = true;

    private readonly NewsletterDispatchSignal _signal = new();
    private readonly NewsletterIssueService _sut;

    public NewsletterIssueServiceTests()
    {
        _issues.Setup(r => r.GetByIdForAdminAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => _issue);
        _issues.Setup(r => r.AddAsync(It.IsAny<NewsletterIssue>(), It.IsAny<CancellationToken>()))
            .Callback<NewsletterIssue, CancellationToken>((i, _) => { i.Id = 3; _issue = i; })
            .Returns(Task.CompletedTask);
        _issues.Setup(r => r.UpdateAsync(It.IsAny<NewsletterIssue>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _deliveries.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<NewsletterDelivery>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<NewsletterDelivery>, CancellationToken>((rows, _) => _queued.AddRange(rows))
            .Returns(Task.CompletedTask);
        _deliveries.Setup(r => r.CountAsync(It.IsAny<Expression<Func<NewsletterDelivery, bool>>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<NewsletterDelivery, bool>>? p, CancellationToken _) =>
                p is null ? _queued.Count : _queued.Count(d => p.Compile()(d)));

        _subscribers.Setup(r => r.SelectForAdminPagedAsync(
                It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<Expression<Func<Subscriber, Subscriber>>>(),
                It.IsAny<Expression<Func<Subscriber, bool>>?>(),
                It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int page, int size, Expression<Func<Subscriber, Subscriber>> shape,
                           Expression<Func<Subscriber, bool>>? predicate, bool _, CancellationToken __) =>
                _audience.Where(s => predicate is null || predicate.Compile()(s))
                         .OrderBy(s => s.Id)
                         .Skip((page - 1) * size).Take(size)
                         .Select(shape.Compile())
                         .ToList());

        _subscribers.Setup(r => r.CountForAdminAsync(It.IsAny<Expression<Func<Subscriber, bool>>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Subscriber, bool>>? p, CancellationToken _) =>
                p is null ? _audience.Count : _audience.Count(s => p.Compile()(s)));

        _types.Setup(r => r.GetAsync(It.IsAny<Expression<Func<MailTemplateType, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => _templateExists ? new MailTemplateType { Id = 6, Code = MailTemplateDefinitions.NewsletterIssue } : null);
        _templates.Setup(r => r.AnyAsync(It.IsAny<Expression<Func<MailTemplate, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => _templateExists);

        _mail.Setup(m => m.SendAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Callback<string, string?, string?, object, CancellationToken>((type, _, to, payload, _) => { _sent.Add((type, to, payload)); _journal.Mail(); })
            .ReturnsAsync(() => _outcome);

        _uow.SetupGet(u => u.Subscribers).Returns(_subscribers.Object);
        _uow.SetupGet(u => u.NewsletterIssues).Returns(_issues.Object);
        _uow.SetupGet(u => u.NewsletterDeliveries).Returns(_deliveries.Object);
        _uow.SetupGet(u => u.MailTemplateTypes).Returns(_types.Object);
        _uow.SetupGet(u => u.MailTemplates).Returns(_templates.Object);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Newsletter:UnsubscribeUrl"] = UnsubscribeUrl
        }).Build();

        var clock = Mock.Of<IClock>(c => c.UtcNow == Now);
        _sut = new NewsletterIssueService(_uow.Object, _mail.Object, config, _signal, _journal.Logger(clock), clock);
    }

    private static NewsletterIssue Issue(string status = NewsletterIssueStatuses.Draft, bool isActive = true, bool isDeleted = false)
        => new()
        {
            Id = 3,
            Subject = "Eylül notları",
            Body = "<p>Merhaba</p>",
            Status = status,
            IsActive = isActive,
            IsDeleted = isDeleted
        };

    private void Audience(params Subscriber[] rows)
    {
        _audience.Clear();
        _audience.AddRange(rows);
    }

    private static Subscriber Sub(int id, bool confirmed = true, bool isActive = true, bool isDeleted = false)
        => new()
        {
            Id = id,
            Email = $"okur{id}@ornek.test",
            ConfirmedAt = confirmed ? Now.AddDays(-1) : null,
            IsActive = isActive,
            IsDeleted = isDeleted
        };


    [Fact]
    public async Task Yeni_bulten_taslak_olarak_acilir()
    {
        _issue = null;

        var result = await _sut.CreateAsync(new CreateNewsletterIssueDto { Subject = "  Eylül  ", Body = "<p>x</p>" }, 1);

        result.Success.Should().BeTrue();
        result.Data!.Status.Should().Be(NewsletterIssueStatuses.Draft);
        result.Data.Subject.Should().Be("Eylül", "konu kırpılarak saklanmalı");
    }

    [Theory]
    [InlineData("", "<p>x</p>")]
    [InlineData("   ", "<p>x</p>")]
    [InlineData("Konu", "")]
    [InlineData("Konu", "   ")]
    public async Task Konusuz_ya_da_govdesiz_bulten_kabul_edilmez(string subject, string body)
    {
        _issue = null;

        var result = await _sut.CreateAsync(new CreateNewsletterIssueDto { Subject = subject, Body = body }, 1);

        result.Success.Should().BeFalse();
        _issue.Should().BeNull("geçersiz bülten hiç kaydedilmemeli");
    }

    [Fact]
    public async Task Dagitima_verilmis_bulten_duzenlenemez()
    {
        _issue = Issue(NewsletterIssueStatuses.Sending);

        var result = await _sut.UpdateAsync(new UpdateNewsletterIssueDto { Id = 3, Subject = "Yeni", Body = "<p>y</p>" }, 1);

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(409);
        _issue.Subject.Should().Be("Eylül notları", "metin dokunulmamış kalmalı");
    }


    [Fact]
    public async Task Dagitima_verilince_her_dogrulanmis_adres_icin_satir_acilir()
    {
        _issue = Issue();
        Audience(Sub(1), Sub(2), Sub(3));

        var result = await _sut.QueueAsync(3, 9);

        result.Success.Should().BeTrue();
        _queued.Should().HaveCount(3);
        _queued.Should().OnlyContain(d => d.NewsletterIssueId == 3 && d.Status == NewsletterDeliveryStatuses.Pending);
        _queued.Select(d => d.Email).Should().BeEquivalentTo("okur1@ornek.test", "okur2@ornek.test", "okur3@ornek.test");
        _issue!.Status.Should().Be(NewsletterIssueStatuses.Sending);
        _issue.QueuedAt.Should().Be(Now);
        _issue.RecipientCount.Should().Be(3);
    }

    [Fact]
    public async Task Dogrulanmamis_pasif_ve_silinmis_adresler_listeye_girmez()
    {
        _issue = Issue();
        Audience(Sub(1), Sub(2, confirmed: false), Sub(3, isActive: false), Sub(4, isDeleted: true), Sub(5));

        await _sut.QueueAsync(3, 9);

        _queued.Select(d => d.SubscriberId).Should().BeEquivalentTo([1, 5]);
    }

    [Fact]
    public async Task Alici_listesi_sayfa_boyutunu_astiginda_tamami_toplanir()
    {
        _issue = Issue();
        Audience([.. Enumerable.Range(1, NewsletterIssueService.RecipientPageSize + 17).Select(i => Sub(i))]);

        await _sut.QueueAsync(3, 9);

        _queued.Should().HaveCount(NewsletterIssueService.RecipientPageSize + 17, "sayfalama alıcı kaybetmemeli");
        _queued.Select(d => d.SubscriberId).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task Ikinci_kez_dagitima_verilemez()
    {
        _issue = Issue(NewsletterIssueStatuses.Sending);
        Audience(Sub(1));

        var result = await _sut.QueueAsync(3, 9);

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(409);
        _queued.Should().BeEmpty("ikinci kuyruk aynı adrese ikinci posta demek olurdu");
    }

    [Fact]
    public async Task Silinmis_bulten_dagitima_verilemez()
    {
        _issue = Issue(isDeleted: true);
        Audience(Sub(1));

        var result = await _sut.QueueAsync(3, 9);

        result.Success.Should().BeFalse();
        _queued.Should().BeEmpty();
    }

    [Fact]
    public async Task Dogrulanmis_abone_yoksa_kuyruk_kurulmaz()
    {
        _issue = Issue();
        Audience(Sub(1, confirmed: false));

        var result = await _sut.QueueAsync(3, 9);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Contain("Doğrulanmış abone yok");
        _issue!.Status.Should().Be(NewsletterIssueStatuses.Draft, "reddedilen kuyruk durumu değiştirmemeli");
    }

    [Fact]
    public async Task Sablon_yoksa_kuyruk_hic_kurulmaz()
    {
        _issue = Issue();
        Audience(Sub(1), Sub(2));
        _templateExists = false;

        var result = await _sut.QueueAsync(3, 9);

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(500);
        _queued.Should().BeEmpty("şablonsuz gönderim alıcı alıcı yanmak yerine hiç başlamamalı");
    }

    [Fact]
    public async Task Cikis_adresi_yapilandirilmamissa_kuyruk_kurulmaz()
    {
        var clock = Mock.Of<IClock>(c => c.UtcNow == Now);
        var service = new NewsletterIssueService(
            _uow.Object, _mail.Object, new ConfigurationBuilder().Build(), _signal,
            new ActivityLogger(Mock.Of<ILogService>(), Mock.Of<IHttpContextAccessor>(), clock), clock);

        _issue = Issue();
        Audience(Sub(1));

        var result = await service.QueueAsync(3, 9);

        result.Success.Should().BeFalse();
        _queued.Should().BeEmpty("çıkış bağlantısı olmadan giden bülten izinsiz listeye dönerdi");
    }

    [Fact]
    public async Task Dondurma_ve_sayim_ayni_suzgeci_kullanir()
    {
        Audience(Sub(1), Sub(2, confirmed: false), Sub(3, isDeleted: true), Sub(4));

        var count = await _sut.GetAudienceCountAsync();

        _issue = Issue();
        await _sut.QueueAsync(3, 9);

        count.Data.Should().Be(2);
        _queued.Should().HaveCount(count.Data, "gösterilen sayı ile dondurulan liste ayrışmamalı");
    }


    [Fact]
    public async Task Taslakta_ilerleme_sorgusu_dagitim_tablosuna_hic_gitmez()
    {
        _issue = Issue();

        var result = await _sut.GetProgressAsync(3);

        result.Data!.PendingCount.Should().Be(0);
        _deliveries.Verify(r => r.CountAsync(It.IsAny<Expression<Func<NewsletterDelivery, bool>>?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Deneme_gonderimi_dagitim_satiri_acmaz_ve_durumu_degistirmez()
    {
        _issue = Issue();

        var result = await _sut.SendTestAsync(3, "  Deneme@Ornek.Test ");

        result.Success.Should().BeTrue();
        _sent.Should().ContainSingle();
        _sent[0].Type.Should().Be(MailTemplateDefinitions.NewsletterIssue);
        _sent[0].To.Should().Be("deneme@ornek.test", "adres kırpılıp küçük harfe indirilmeli");
        ((NewsletterIssueMailDto)_sent[0].Payload).Body.Should().Be("<p>Merhaba</p>");
        _queued.Should().BeEmpty();
        _issue!.Status.Should().Be(NewsletterIssueStatuses.Draft);
    }

    [Fact]
    public async Task Denemenin_cikis_baglantisi_jeton_tasimaz()
    {
        _issue = Issue();

        await _sut.SendTestAsync(3, "deneme@ornek.test");

        ((NewsletterIssueMailDto)_sent[0].Payload).UnsubscribeUrl
            .Should().Be(UnsubscribeUrl, "deneme alıcısı abone olmayabilir; ona gerçek jeton üretmek listeye ait olmayan bir kimlik bilgisi yaratırdı");
    }

    [Fact]
    public async Task Deneme_kaydi_gonderimden_sonra_yazilir()
    {
        _issue = Issue();

        await _sut.SendTestAsync(3, "deneme@ornek.test");

        _journal.Events.Should().Equal("mail", "log:Information");
        _journal.Logs[0].Message.Should().Contain("sayı #3").And.Contain("gönderildi");
    }

    [Fact]
    public async Task Gonderilemeyen_deneme_hata_olarak_kayda_gecer()
    {
        _issue = Issue();
        _outcome = Result.Fail("Posta gönderilemedi.", "SMTP hatası (newsletter-issue): 535", 502);

        var result = await _sut.SendTestAsync(3, "deneme@ornek.test");

        result.IsFailure.Should().BeTrue();
        _journal.Events.Should().Equal("mail", "log:Error");
        _journal.Logs[0].Message.Should().Contain("gönderilemedi").And.Contain("535");
    }

    [Theory]
    [InlineData("")]
    [InlineData("nokta-yok")]
    [InlineData("iki@@at.test")]
    public async Task Gecersiz_deneme_adresi_reddedilir(string email)
    {
        _issue = Issue();

        var result = await _sut.SendTestAsync(3, email);

        result.Success.Should().BeFalse();
        _sent.Should().BeEmpty();
    }

    [Fact]
    public async Task Dagitim_surerken_pasife_alma_engellenmez()
    {
        _issue = Issue(NewsletterIssueStatuses.Sending);

        var result = await _sut.ToggleActiveAsync(3, 9);

        result.Success.Should().BeTrue();
        _issue!.IsActive.Should().BeFalse("aktiflik anahtarı süren bir dağıtımın duraklatma düğmesidir");
    }
}
