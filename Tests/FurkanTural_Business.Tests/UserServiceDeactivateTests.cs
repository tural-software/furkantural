using FluentAssertions;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Business.Helpers;
using FurkanTural_Business.Services.Concrete;
using FurkanTural_Domain.Entities;
using Microsoft.AspNetCore.Http;
using Moq;

namespace FurkanTural_Business.Tests;

/// <summary>Planın C maddesi: kullanıcı yalnızca kendi hesabını, yalnızca kapatma yönünde değiştirebilir.</summary>
public class UserServiceDeactivateTests
{
    private const string Password = "dogru-parola";
    private const string Hashed = "hash:dogru-parola";

    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly UserService _sut;

    public UserServiceDeactivateTests()
    {
        _hasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string given, string stored) => given == Password && stored == Hashed);

        _uow.SetupGet(u => u.Users).Returns(_users.Object);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var clock = Mock.Of<IClock>(c => c.UtcNow == new DateTime(2026, 8, 25, 9, 0, 0, DateTimeKind.Utc));

        _sut = new UserService(
            _uow.Object,
            _hasher.Object,
            new ActivityLogger(Mock.Of<ILogService>(), Mock.Of<IHttpContextAccessor>(), clock),
            Mock.Of<IUserFriendService>(),
            clock);
    }

    private User Account(int id = 7)
    {
        var user = new User { Id = id, Username = "deneme", Password = Hashed, IsActive = true, IsDeleted = false };
        _users.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        return user;
    }

    private void NoAccount()
        => _users.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

    [Fact]
    public async Task Dogru_parolayla_hesap_kapanir()
    {
        var user = Account();

        var result = await _sut.DeactivateMyAccountAsync(7, Password);

        result.Success.Should().BeTrue();
        user.IsActive.Should().BeFalse();
        user.IsDeleted.Should().BeFalse("kapatmak silmek değildir");
        user.UpdatedBy.Should().Be(7);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Yanlis_parola_hesabi_kapatmaz()
    {
        var user = Account();

        var result = await _sut.DeactivateMyAccountAsync(7, "yanlis-parola");

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(401);
        user.IsActive.Should().BeTrue();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Parolasiz_istek_reddedilir(string? password)
    {
        Account();

        var result = await _sut.DeactivateMyAccountAsync(7, password);

        result.IsFailure.Should().BeTrue();
        _users.Verify(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Zaten_kapali_hesap_bulunamaz()
    {
        NoAccount();

        var result = await _sut.DeactivateMyAccountAsync(7, Password);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(404, "küresel süzgeç pasif ve silinmiş kaydı zaten göstermez");
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Parolasi_olmayan_hesap_kapatilamaz()
    {
        var user = Account();
        user.Password = null;

        var result = await _sut.DeactivateMyAccountAsync(7, Password);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(401);
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Kapatma_tek_yonludur_hesabi_geri_acmaz()
    {
        var user = Account();

        await _sut.DeactivateMyAccountAsync(7, Password);
        _users.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        var ikinci = await _sut.DeactivateMyAccountAsync(7, Password);

        user.IsActive.Should().BeFalse();
        ikinci.IsFailure.Should().BeTrue("kapalı hesap süzgecin arkasındadır, ikinci çağrı onu bulamaz");
    }
}
