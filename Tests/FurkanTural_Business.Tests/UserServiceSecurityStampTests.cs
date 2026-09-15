using System.Linq.Expressions;
using FluentAssertions;
using FurkanTural_Application.DTOs.User;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Business.Helpers;
using FurkanTural_Business.Services.Concrete;
using FurkanTural_Domain.Entities;
using Microsoft.AspNetCore.Http;
using Moq;

namespace FurkanTural_Business.Tests;

public class UserServiceSecurityStampTests
{
    private const string EskiDamga = "eski-damga";

    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IRepository<PushSubscription>> _subscriptions = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly UserService _sut;

    public UserServiceSecurityStampTests()
    {
        _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("yeni-ozet");
        _hasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

        _uow.SetupGet(u => u.Users).Returns(_users.Object);
        _uow.SetupGet(u => u.PushSubscriptions).Returns(_subscriptions.Object);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _subscriptions.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<PushSubscription, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PushSubscription>().AsEnumerable());
        _subscriptions.Setup(r => r.DeleteRangeAsync(It.IsAny<IEnumerable<PushSubscription>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var clock = Mock.Of<IClock>(c => c.UtcNow == new DateTime(2026, 9, 15, 9, 0, 0, DateTimeKind.Utc));

        _sut = new UserService(
            _uow.Object,
            _hasher.Object,
            new ActivityLogger(Mock.Of<ILogService>(), Mock.Of<IHttpContextAccessor>(), clock),
            Mock.Of<IUserFriendService>(),
            clock);
    }

    private User Account(int id = 7, int roleId = 2)
    {
        var user = new User
        {
            Id = id,
            Username = "deneme",
            Password = "eski-ozet",
            RoleId = roleId,
            SecurityStamp = EskiDamga,
            IsActive = true,
            IsDeleted = false
        };

        _users.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _users.Setup(r => r.GetByIdForAdminAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        return user;
    }

    [Fact]
    public async Task Aktiflik_degisince_damga_tazelenir()
    {
        var user = Account();

        await _sut.ToggleActiveAsync(7, updatedBy: 1);

        user.SecurityStamp.Should().NotBe(EskiDamga,
            "yasaklanan kullanıcının elindeki jeton süresi dolana kadar geçerli kalırdı; damga tazelenince düşer");
    }

    [Fact]
    public async Task Rol_degisince_damga_tazelenir()
    {
        var user = Account(roleId: 2);

        await _sut.UpdateAsync(new UpdateUserDto { Id = 7, Username = "deneme", RoleId = 3 });

        user.SecurityStamp.Should().NotBe(EskiDamga,
            "yöneticilikten düşürülen biri eski jetonuyla bir süre daha yönetici kalırdı");
    }

    [Fact]
    public async Task Yalnizca_gorunen_ad_degisince_damga_korunur()
    {
        var user = Account(roleId: 2);

        await _sut.UpdateAsync(new UpdateUserDto { Id = 7, Username = "deneme", RoleId = 2, DisplayName = "Yeni Ad" });

        user.SecurityStamp.Should().Be(EskiDamga,
            "zararsız bir alan düzeltmesi kullanıcıyı oturumdan atmamalı; damga yalnız yetki ya da parola " +
            "değişiminde tazelenir");
    }

    [Fact]
    public async Task Hesap_kapatilinca_damga_tazelenir()
    {
        var user = Account();

        await _sut.DeactivateMyAccountAsync(7, "dogru-parola");

        user.SecurityStamp.Should().NotBe(EskiDamga,
            "hesabını kapatan kullanıcının açık jetonu kapanışı hükümsüz bırakırdı");
    }

    [Fact]
    public async Task Silinince_damga_tazelenir()
    {
        var user = Account();

        await _sut.DeleteAsync(7, deletedBy: 1);

        user.SecurityStamp.Should().NotBe(EskiDamga,
            "silinen hesabın jetonu de facto geçerli kalmamalı");
    }
}
