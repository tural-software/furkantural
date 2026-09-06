using System.Linq.Expressions;
using FluentAssertions;
using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Business.Services.Concrete;
using FurkanTural_Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FurkanTural_Business.Tests;

/// <summary>Pano toplayıcısı yirmi bir varlığın özetini uçların yol adıyla anahtarlar ve dört sayacı süzgeçli sayımla alır. Süzgeçlerin anlamı burada derlenip doğrulanır: okunmamış = silinmemiş ve okunmamış; bekleyen = silinmemiş ve Pending; aktif kullanıcı = silinmemiş, aktif ve pencere başından beri görülmüş; haftalık = silinmemiş ve CreatedAt pencere içinde (bitiş dışlanır). Pencere 1-90 güne sıkıştırılır.</summary>
public class AdminDashboardServiceTests
{
    private static readonly DateTime Today = new(2026, 9, 4);

    private readonly Mock<IUnitOfWork> _uow = new() { DefaultValue = DefaultValue.Mock };
    private readonly Mock<IRepository<Contact>> _contacts = new();
    private readonly Mock<IRepository<Report>> _reports = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IBlogRepository> _blogs = new();
    private readonly Mock<ISubscriberRepository> _subscribers = new();
    private readonly List<Expression<Func<User, bool>>> _userPredicates = [];
    private readonly List<Expression<Func<Blog, bool>>> _blogPredicates = [];
    private Expression<Func<Contact, bool>>? _unreadPredicate;
    private Expression<Func<Report, bool>>? _pendingPredicate;

    public AdminDashboardServiceTests()
    {
        _contacts.Setup(r => r.CountForAdminAsync(It.IsAny<Expression<Func<Contact, bool>>?>(), It.IsAny<CancellationToken>()))
            .Callback<Expression<Func<Contact, bool>>?, CancellationToken>((p, _) => _unreadPredicate ??= p)
            .ReturnsAsync(3);
        _reports.Setup(r => r.CountForAdminAsync(It.IsAny<Expression<Func<Report, bool>>?>(), It.IsAny<CancellationToken>()))
            .Callback<Expression<Func<Report, bool>>?, CancellationToken>((p, _) => _pendingPredicate = p)
            .ReturnsAsync(2);
        _users.Setup(r => r.CountForAdminAsync(It.IsAny<Expression<Func<User, bool>>?>(), It.IsAny<CancellationToken>()))
            .Callback<Expression<Func<User, bool>>?, CancellationToken>((p, _) => _userPredicates.Add(p!))
            .ReturnsAsync(4);
        _blogs.Setup(r => r.CountForAdminAsync(It.IsAny<Expression<Func<Blog, bool>>?>(), It.IsAny<CancellationToken>()))
            .Callback<Expression<Func<Blog, bool>>?, CancellationToken>((p, _) => _blogPredicates.Add(p!))
            .ReturnsAsync(2);
        _subscribers.Setup(r => r.CountForAdminAsync(It.IsAny<Expression<Func<Subscriber, bool>>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        foreach (var repo in new object[] { _contacts, _reports, _users, _blogs, _subscribers })
            SetupSummary(repo);

        _uow.SetupGet(u => u.Contacts).Returns(_contacts.Object);
        _uow.SetupGet(u => u.Reports).Returns(_reports.Object);
        _uow.SetupGet(u => u.Users).Returns(_users.Object);
        _uow.SetupGet(u => u.Blogs).Returns(_blogs.Object);
        _uow.SetupGet(u => u.Subscribers).Returns(_subscribers.Object);
    }

    private static void SetupSummary(object mock)
    {
        switch (mock)
        {
            case Mock<IRepository<Contact>> c: c.Setup(r => r.GetAdminSummaryAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new EntitySummaryDto(7, null)); break;
            case Mock<IRepository<Report>> r: r.Setup(x => x.GetAdminSummaryAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new EntitySummaryDto(5, null)); break;
            case Mock<IUserRepository> u: u.Setup(x => x.GetAdminSummaryAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new EntitySummaryDto(12, null)); break;
            case Mock<IBlogRepository> b: b.Setup(x => x.GetAdminSummaryAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new EntitySummaryDto(28, new DateTime(2026, 9, 1))); break;
            case Mock<ISubscriberRepository> s: s.Setup(x => x.GetAdminSummaryAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new EntitySummaryDto(9, null)); break;
        }
    }

    private AdminDashboardService Sut() => new(_uow.Object, NullLogger<AdminDashboardService>.Instance);

    [Fact]
    public async Task Ozetler_yol_adiyla_anahtarlanir_ve_verilmeyen_depo_bos_ozetle_gelir()
    {
        var result = await Sut().GetAsync(Today, 7);

        result.Success.Should().BeTrue();
        result.Data!.Summaries.Should().HaveCount(21, "panelin yirmi bir modülü var; her biri için bir anahtar");
        result.Data.Summaries["blog"].Should().Be(new EntitySummaryDto(28, new DateTime(2026, 9, 1)));
        result.Data.Summaries["user"].TotalCount.Should().Be(12);
        result.Data.Summaries.Keys.Should().Contain(["blogimage", "mailtemplate", "log"],
            "anahtarlar API uçlarının yol adıdır; panel bunlarla modül kartlarını eşler");
        result.Data.Summaries["category"].Should().BeNull("kurulmamış depo Moq'ta boş döner; gerçek depoda her varlık bir özet verir");
    }

    [Fact]
    public async Task Sayaclar_ve_suzgecleri_dogru_anlami_tasir()
    {
        var result = await Sut().GetAsync(Today, 7);

        result.Data!.UnreadContacts.Should().Be(3);
        result.Data.PendingReports.Should().Be(2);
        result.Data.ActiveUsers.Should().Be(4);
        result.Data.ThisWeek.Should().Be(new AdminWeeklyCountsDto(2, 4, 3, 1));

        var unread = _unreadPredicate!.Compile();
        unread(new Contact { IsRead = false }).Should().BeTrue();
        unread(new Contact { IsRead = true }).Should().BeFalse();
        unread(new Contact { IsRead = false, IsDeleted = true }).Should().BeFalse("silinmiş mesaj bekleyen iş değildir");

        var pending = _pendingPredicate!.Compile();
        pending(new Report { Status = "Pending" }).Should().BeTrue();
        pending(new Report { Status = "Reviewed" }).Should().BeFalse();
        pending(new Report { Status = "Pending", IsDeleted = true }).Should().BeFalse();

        var active = _userPredicates[0].Compile();
        active(new User { IsActive = true, LastSeenAt = Today.AddDays(-6) }).Should().BeTrue("pencerenin ilk günü dahil");
        active(new User { IsActive = true, LastSeenAt = Today.AddDays(-7) }).Should().BeFalse();
        active(new User { IsActive = false, LastSeenAt = Today }).Should().BeFalse("kapatılmış hesap aktif sayılmaz");
        active(new User { IsActive = true, LastSeenAt = null }).Should().BeFalse();
    }

    [Fact]
    public async Task Haftalik_pencereler_bitisik_ve_bitis_dislayan_araliklardir()
    {
        await Sut().GetAsync(Today, 7);

        var thisWeek = _blogPredicates[0].Compile();
        var lastWeek = _blogPredicates[1].Compile();

        thisWeek(new Blog { CreatedAt = Today.AddDays(-6) }).Should().BeTrue();
        thisWeek(new Blog { CreatedAt = Today.AddHours(23) }).Should().BeTrue("bugünün tamamı bu haftadır");
        thisWeek(new Blog { CreatedAt = Today.AddDays(-7) }).Should().BeFalse();
        lastWeek(new Blog { CreatedAt = Today.AddDays(-7) }).Should().BeTrue();
        lastWeek(new Blog { CreatedAt = Today.AddDays(-13) }).Should().BeTrue();
        lastWeek(new Blog { CreatedAt = Today.AddDays(-6) }).Should().BeFalse("iki pencere çakışmaz, bir gün iki kez sayılmaz");
        thisWeek(new Blog { CreatedAt = Today, IsDeleted = true }).Should().BeFalse();
    }

    [Fact]
    public async Task Pencere_bir_ile_doksan_gune_sikistirilir()
    {
        await Sut().GetAsync(Today, 0);
        var oneDay = _userPredicates[0].Compile();
        oneDay(new User { IsActive = true, LastSeenAt = Today }).Should().BeTrue();
        oneDay(new User { IsActive = true, LastSeenAt = Today.AddDays(-1) }).Should().BeFalse("pencere en az bir gündür: yalnızca bugün");

        _userPredicates.Clear();
        await Sut().GetAsync(Today, 1000);
        var capped = _userPredicates[0].Compile();
        capped(new User { IsActive = true, LastSeenAt = Today.AddDays(-89) }).Should().BeTrue();
        capped(new User { IsActive = true, LastSeenAt = Today.AddDays(-90) }).Should().BeFalse("pencere en çok doksan gündür");
    }

    [Fact]
    public async Task Dusen_sorgu_yalniz_kendi_parcasini_goturur()
    {
        _blogs.Setup(r => r.GetAdminSummaryAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("özet sorgusu düştü"));
        _reports.Setup(r => r.CountForAdminAsync(It.IsAny<Expression<Func<Report, bool>>?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("sayaç sorgusu düştü"));

        var result = await Sut().GetAsync(Today, 7);

        result.Success.Should().BeTrue("tek bir sorgunun düşmesi yanıtın tamamını götürmez");
        result.Data!.Summaries.Should().NotContainKey("blog", "okunamayan özet sözlükte hiç yer almaz, kart boş çizilir");
        result.Data.Summaries.Should().ContainKey("user", "yanındaki özetler etkilenmez");
        result.Data.PendingReports.Should().BeNull("okunamayan sayaç boştur; sıfır yazmak uydurma olurdu");
        result.Data.UnreadContacts.Should().Be(3);
        result.Data.ActiveUsers.Should().Be(4);
        result.Data.ThisWeek.Should().Be(new AdminWeeklyCountsDto(2, 4, 3, 1), "haftalık sayaçlar ayrı sorgulardır, şikayet sayacıyla düşmez");
    }

    [Fact]
    public async Task Haftalik_sayaclardan_biri_duserse_digerleri_kalir()
    {
        _subscribers.Setup(r => r.CountForAdminAsync(It.IsAny<Expression<Func<Subscriber, bool>>?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("abone sayacı düştü"));

        var result = await Sut().GetAsync(Today, 7);

        result.Data!.ThisWeek.Should().Be(new AdminWeeklyCountsDto(2, 4, 3, null));
        result.Data.LastWeek.Should().Be(new AdminWeeklyCountsDto(2, 4, 3, null));
    }

    [Fact]
    public async Task Istemci_vazgecerse_yanit_kismi_degil_iptal_olur()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        _blogs.Setup(r => r.GetAdminSummaryAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var act = () => Sut().GetAsync(Today, 7, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>(
            "istemci isteği yarıda kesmişse eksik bir pano üretmenin anlamı yok; iptal yukarı çıkar");
    }
}
