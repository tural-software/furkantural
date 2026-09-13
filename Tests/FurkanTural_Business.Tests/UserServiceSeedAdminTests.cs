using System.Linq.Expressions;
using FluentAssertions;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Business.Helpers;
using FurkanTural_Business.Services.Concrete;
using FurkanTural_Domain.Entities;
using Microsoft.AspNetCore.Http;
using Moq;

namespace FurkanTural_Business.Tests;

public class UserServiceSeedAdminTests
{
    private const string Parola = "P@ss1234";

    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly List<User> _eklenen = [];
    private readonly UserService _sut;

    public UserServiceSeedAdminTests()
    {
        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns((string p) => "hash:" + p);

        _users.Setup(r => r.AnyAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _users.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => _eklenen.Add(u))
            .Returns(Task.CompletedTask);

        _uow.SetupGet(u => u.Users).Returns(_users.Object);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var clock = Mock.Of<IClock>(c => c.UtcNow == new DateTime(2026, 9, 13, 12, 0, 0, DateTimeKind.Utc));

        _sut = new UserService(
            _uow.Object,
            hasher.Object,
            new ActivityLogger(Mock.Of<ILogService>(), Mock.Of<IHttpContextAccessor>(), clock),
            Mock.Of<IUserFriendService>(),
            clock);
    }

    private void KayitliKullaniciSayisi(int count)
        => _users.Setup(r => r.CountForAdminAsync(It.IsAny<Expression<Func<User, bool>>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(count);

    [Fact]
    public async Task Yalnizca_pasif_ya_da_silinmis_kullanici_varken_ilk_yonetici_kurulamaz()
    {
        KayitliKullaniciSayisi(1);

        var result = await _sut.SeedAdminAsync("yabanci", Parola);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(409);
        _eklenen.Should().BeEmpty("süzgeçli okuma bu kullanıcıyı görmese de sistem kurulmuştur; uç anonim bir yönetici açmamalı");
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _users.Verify(r => r.AnyAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()), Times.Never,
            "varlık kontrolü küresel süzgeçten geçer ve pasif ya da silinmiş kullanıcıyı saymaz");
    }

    [Fact]
    public async Task Hic_kullanici_yokken_ilk_yonetici_kurulur()
    {
        KayitliKullaniciSayisi(0);

        var result = await _sut.SeedAdminAsync("admin", Parola);

        result.Success.Should().BeTrue();
        var admin = _eklenen.Should().ContainSingle().Subject;
        admin.RoleId.Should().Be(1);
        admin.Password.Should().Be("hash:" + Parola);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
