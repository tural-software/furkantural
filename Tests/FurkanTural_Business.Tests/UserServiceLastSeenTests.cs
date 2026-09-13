using FluentAssertions;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Business.Helpers;
using FurkanTural_Business.Services.Concrete;
using FurkanTural_Domain.Entities;
using Microsoft.AspNetCore.Http;
using Moq;

namespace FurkanTural_Business.Tests;

public class UserServiceLastSeenTests
{
    private static readonly DateTime Now = new(2026, 9, 13, 12, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly UserService _sut;

    public UserServiceLastSeenTests()
    {
        _uow.SetupGet(u => u.Users).Returns(_users.Object);
        var clock = Mock.Of<IClock>(c => c.UtcNow == Now);
        _sut = new UserService(_uow.Object, Mock.Of<IPasswordHasher>(),
            new ActivityLogger(Mock.Of<ILogService>(), Mock.Of<IHttpContextAccessor>(), clock),
            Mock.Of<IUserFriendService>(), clock);
    }

    [Fact]
    public async Task Son_gorulme_hedefli_yazmayla_kaydedilir()
    {
        var result = await _sut.UpdateLastSeenAsync(7);

        result.Should().Be(Now);
        _users.Verify(r => r.TouchLastSeenAsync(7, Now, It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Son_gorulme_satiri_okuyup_geri_yazmaz()
    {
        await _sut.UpdateLastSeenAsync(7);

        _users.Verify(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never(),
            "satırı okuyup bütün sütunlarıyla geri yazmak aynı anda yöneticinin yaptığı değişikliği ezer");
        _users.Verify(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never());
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never(),
            "kaydetme yoluna giren yazma canlı bildirim kancasını tetikler; son görülme zamanı yöneticiye haber üretmemeli");
    }
}
