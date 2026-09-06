using System.Linq.Expressions;
using FluentAssertions;
using FurkanTural_Application.DTOs.Subscriber;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Business.Helpers;
using FurkanTural_Business.Services.Concrete;
using FurkanTural_Domain.Entities;
using Microsoft.AspNetCore.Http;
using Moq;

namespace FurkanTural_Business.Tests;

/// <summary>Abone kayıtlarının yönetim tarafı. Ziyaretçinin gördüğü çift onaylı abonelik akışı burada değil <see cref="NewsletterService"/>'tedir.</summary>
public class SubscriberServiceTests
{
    private const string Email = "abone@ornek.test";

    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ISubscriberRepository> _subscribers = new();
    private readonly List<Subscriber> _added = [];
    private readonly List<Subscriber> _restored = [];
    private readonly SubscriberService _sut;

    public SubscriberServiceTests()
    {
        _subscribers.Setup(r => r.AddAsync(It.IsAny<Subscriber>(), It.IsAny<CancellationToken>()))
            .Callback<Subscriber, CancellationToken>((s, _) => _added.Add(s))
            .Returns(Task.CompletedTask);

        _subscribers.Setup(r => r.RestoreAsync(It.IsAny<Subscriber>(), It.IsAny<CancellationToken>()))
            .Callback<Subscriber, CancellationToken>((s, _) =>
            {
                s.IsDeleted = false;
                s.IsActive = true;
                s.DeletedAt = null;
                _restored.Add(s);
            })
            .Returns(Task.CompletedTask);

        _uow.SetupGet(u => u.Subscribers).Returns(_subscribers.Object);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var clock = Mock.Of<IClock>(c => c.UtcNow == new DateTime(2026, 8, 25, 9, 0, 0, DateTimeKind.Utc));
        _sut = new SubscriberService(
            _uow.Object,
            new ActivityLogger(Mock.Of<ILogService>(), Mock.Of<IHttpContextAccessor>(), clock));
    }

    private void RowIs(Subscriber? row)
        => _subscribers.Setup(r => r.GetByEmailForAdminAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(row);

    private void ById(Subscriber? row)
        => _subscribers.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(row);

    private static Subscriber Row(bool isActive, bool isDeleted, int id = 4)
        => new() { Id = id, Email = Email, IsActive = isActive, IsDeleted = isDeleted };

    [Fact]
    public async Task Yonetici_kaydinda_silinmis_satir_adiyla_bildirilir()
    {
        RowIs(Row(isActive: false, isDeleted: true, id: 12));

        var result = await _sut.CreateAsync(new CreateSubscriberDto { Email = Email });

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(409);
        result.Errors[0].Should().Contain("#12").And.Contain("geri yükleyin");
        _added.Should().BeEmpty();
    }

    [Fact]
    public async Task Yonetici_kaydinda_duran_satir_da_bildirilir()
    {
        RowIs(Row(isActive: true, isDeleted: false, id: 5));

        var result = await _sut.CreateAsync(new CreateSubscriberDto { Email = Email });

        result.StatusCode.Should().Be(409);
        result.Errors[0].Should().Contain("#5");
    }

    [Fact]
    public async Task Guncellemede_baska_kaydin_adresi_alinamaz()
    {
        ById(Row(isActive: true, isDeleted: false, id: 3));
        RowIs(Row(isActive: false, isDeleted: true, id: 9));

        var result = await _sut.UpdateAsync(new UpdateSubscriberDto { Id = 3, Email = Email });

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(409);
        _subscribers.Verify(r => r.UpdateAsync(It.IsAny<Subscriber>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Guncellemede_kaydin_kendi_adresi_engel_degildir()
    {
        var row = Row(isActive: true, isDeleted: false, id: 3);
        ById(row);
        RowIs(row);

        var result = await _sut.UpdateAsync(new UpdateSubscriberDto { Id = 3, Email = Email });

        result.Success.Should().BeTrue();
        _subscribers.Verify(r => r.UpdateAsync(row, It.IsAny<CancellationToken>()), Times.Once);
    }
}
