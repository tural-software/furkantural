using FluentAssertions;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Business.Helpers;
using FurkanTural_Business.Services.Concrete;
using FurkanTural_Domain.Entities;
using Microsoft.AspNetCore.Http;
using Moq;

namespace FurkanTural_Business.Tests;

public class ChatMessageServiceReadTests
{
    private static readonly DateTime Now = new(2026, 9, 13, 12, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IChatMessageRepository> _messages = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly ChatMessageService _sut;

    public ChatMessageServiceReadTests()
    {
        _uow.SetupGet(u => u.ChatMessages).Returns(_messages.Object);
        var clock = Mock.Of<IClock>(c => c.UtcNow == Now);
        _sut = new ChatMessageService(_uow.Object, Mock.Of<IUserFriendService>(), Mock.Of<IMessageRateLimiter>(),
            Mock.Of<IMessageProtector>(), Mock.Of<IPresenceTracker>(), Mock.Of<IPushSender>(),
            new ActivityLogger(Mock.Of<ILogService>(), Mock.Of<IHttpContextAccessor>(), clock), clock);
    }

    [Fact]
    public async Task Okundu_isareti_karsi_tarafin_gonderdigi_mesajlara_basilir()
    {
        var result = await _sut.MarkConversationReadAsync(currentUserId: 3, otherUserId: 9);

        result.Success.Should().BeTrue();
        _messages.Verify(r => r.MarkConversationReadAsync(9, 3, Now, It.IsAny<CancellationToken>()), Times.Once(),
            "okundu işareti karşı tarafın gönderdiği ve çağıranın aldığı mesajlara basılır; sıra ters dönerse kullanıcı kendi mesajını okumuş sayılır");
    }

    [Fact]
    public async Task Okundu_isareti_mesajlari_tek_tek_geri_yazmaz()
    {
        await _sut.MarkConversationReadAsync(currentUserId: 3, otherUserId: 9);

        _messages.Verify(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<CancellationToken>()), Times.Never(),
            "mesajları bütün sütunlarıyla geri yazmak şifreli içeriği de yeniden yazar ve aynı anda yapılan düzenlemeyi ezer");
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never(),
            "okundu işareti yöneticiye haber üretmemeli; kaydetme yoluna girerse canlı bildirim kancası tetiklenir");
    }
}
