using System.Linq.Expressions;
using FluentAssertions;
using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Business.Helpers;
using FurkanTural_Business.Services.Concrete;
using FurkanTural_Domain.Entities;
using Microsoft.AspNetCore.Http;
using Moq;

namespace FurkanTural_Business.Tests;

/// <summary>Kullanıcı sayaçlarındaki seenSince süzgeci "son görülme" alt sınırıdır: hiç görülmemiş kullanıcı geçmez, sınırdan eski görülme geçmez, sınır ve sonrası geçer. Üç parametreli eski çağrı süzgeçsiz kalır.</summary>
public class UserServiceSeenSinceTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly UserService _sut;
    private Expression<Func<User, bool>>? _predicate;
    private bool _called;

    public UserServiceSeenSinceTests()
    {
        _users.Setup(r => r.GetAdminStatusCountsAsync(It.IsAny<Expression<Func<User, bool>>?>(), It.IsAny<CancellationToken>()))
            .Callback<Expression<Func<User, bool>>?, CancellationToken>((p, _) => { _predicate = p; _called = true; })
            .ReturnsAsync(new AdminStatusCountsDto(0, 0, 0, 0));

        var uow = new Mock<IUnitOfWork>();
        uow.SetupGet(u => u.Users).Returns(_users.Object);
        var clock = Mock.Of<IClock>(c => c.UtcNow == new DateTime(2026, 9, 4, 9, 0, 0, DateTimeKind.Utc));
        _sut = new UserService(uow.Object, Mock.Of<IPasswordHasher>(),
            new ActivityLogger(Mock.Of<ILogService>(), Mock.Of<IHttpContextAccessor>(), clock),
            Mock.Of<IUserFriendService>(), clock);
    }

    [Fact]
    public async Task SeenSince_son_gorulme_alt_siniridir()
    {
        var since = new DateTime(2026, 8, 29, 0, 0, 0, DateTimeKind.Utc);

        await _sut.GetAdminStatusCountsAsync(new AdminListQuery(), null, since);

        var match = _predicate!.Compile();
        match(new User { LastSeenAt = since }).Should().BeTrue("sınırın kendisi dahil");
        match(new User { LastSeenAt = since.AddDays(3) }).Should().BeTrue();
        match(new User { LastSeenAt = since.AddSeconds(-1) }).Should().BeFalse();
        match(new User { LastSeenAt = null }).Should().BeFalse("hiç görülmemiş kullanıcı aktif değildir");
    }

    [Fact]
    public async Task Eski_imza_suzgecsiz_kalir()
    {
        await _sut.GetAdminStatusCountsAsync(new AdminListQuery(), null);

        _called.Should().BeTrue();
        _predicate.Should().BeNull("süzgeç yokken yüklem de yok; SQL'e gereksiz WHERE inmez");
    }
}
