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

public class UserServicePasswordPolicyTests
{
    private const string Zayif = "1234";
    private const string Gecerli = "P@ss1234";
    private const string EskiOzet = "hash:eski";

    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly List<User> _eklenen = [];
    private readonly List<User> _guncellenen = [];
    private readonly UserService _sut;

    public UserServicePasswordPolicyTests()
    {
        _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns((string p) => "hash:" + p);

        _users.Setup(r => r.AnyAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _users.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => _eklenen.Add(u))
            .Returns(Task.CompletedTask);

        _users.Setup(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => _guncellenen.Add(u))
            .Returns(Task.CompletedTask);

        _uow.SetupGet(u => u.Users).Returns(_users.Object);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var clock = Mock.Of<IClock>(c => c.UtcNow == new DateTime(2026, 8, 28, 9, 0, 0, DateTimeKind.Utc));

        _sut = new UserService(
            _uow.Object,
            _hasher.Object,
            new ActivityLogger(Mock.Of<ILogService>(), Mock.Of<IHttpContextAccessor>(), clock),
            Mock.Of<IUserFriendService>(),
            clock);
    }

    private User Mevcut(int id = 7)
    {
        var user = new User { Id = id, Username = "deneme", Password = EskiOzet, IsActive = true, IsDeleted = false };
        _users.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        return user;
    }

    [Fact]
    public async Task Panelden_zayif_parolayla_kullanici_acilamaz()
    {
        var result = await _sut.CreateAsync(new CreateUserDto { Username = "yeni", Password = Zayif, RoleId = 2 });

        result.IsFailure.Should().BeTrue();
        _eklenen.Should().BeEmpty("politika yalnızca kayıt ucunda çalışırsa panel arka kapı olur");
    }

    [Fact]
    public async Task Panelden_gecerli_parolayla_kullanici_acilir()
    {
        var result = await _sut.CreateAsync(new CreateUserDto { Username = "yeni", Password = Gecerli, RoleId = 2 });

        result.Success.Should().BeTrue();
        _eklenen.Should().ContainSingle();
    }

    [Fact]
    public async Task Panelden_zayif_parolaya_guncelleme_reddedilir()
    {
        var user = Mevcut();

        var result = await _sut.UpdateAsync(new UpdateUserDto { Id = 7, Username = "deneme", Password = Zayif, RoleId = 2 });

        result.IsFailure.Should().BeTrue();
        user.Password.Should().Be(EskiOzet, "reddedilen bir güncelleme hiçbir alanı değiştirmemeli");
        user.Username.Should().Be("deneme");
        _guncellenen.Should().BeEmpty();
    }

    [Fact]
    public async Task Parola_bos_birakilan_guncelleme_politikaya_takilmaz()
    {
        var user = Mevcut();

        var result = await _sut.UpdateAsync(new UpdateUserDto { Id = 7, Username = "yeniad", Password = null, RoleId = 2 });

        result.Success.Should().BeTrue("boş parola alanı 'değiştirme' demektir, 'geçersiz' değil");
        user.Password.Should().Be(EskiOzet);
        user.Username.Should().Be("yeniad");
    }

    [Fact]
    public async Task Ilk_admin_zayif_parolayla_kurulamaz()
    {
        var result = await _sut.SeedAdminAsync("admin", Zayif);

        result.IsFailure.Should().BeTrue();
        _eklenen.Should().BeEmpty();
    }
}
