using FluentAssertions;
using FurkanTural_Application.DTOs.Contact;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using FurkanTural_Business.Services.Concrete;
using FurkanTural_Domain.Constants;
using FurkanTural_Domain.Entities;
using Microsoft.Extensions.Configuration;
using Moq;

namespace FurkanTural_Business.Tests;

public class ContactServiceTests
{
    private static readonly DateTime Now = new(2026, 9, 19, 12, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IRepository<Contact>> _contacts = new();
    private readonly Mock<IMailSender> _mail = new();
    private readonly ActivityJournal _journal = new();
    private readonly Dictionary<string, Result> _outcomes = [];
    private readonly ContactService _sut;

    public ContactServiceTests()
    {
        _contacts.Setup(r => r.AddAsync(It.IsAny<Contact>(), It.IsAny<CancellationToken>()))
            .Callback<Contact, CancellationToken>((c, _) => c.Id = 5)
            .Returns(Task.CompletedTask);

        _mail.Setup(m => m.SendAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string type, string? _, string? _, object _, CancellationToken _) =>
            {
                _journal.Mail();
                return _outcomes.GetValueOrDefault(type, Result.Ok());
            });

        _uow.SetupGet(u => u.Contacts).Returns(_contacts.Object);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var turnstile = new Mock<ITurnstileVerifier>();
        turnstile.Setup(t => t.VerifyAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var throttle = new Mock<IAbuseThrottle>();
        throttle.Setup(t => t.TryRegister(It.IsAny<string>(), It.IsAny<string?>())).Returns(true);

        var clock = Mock.Of<IClock>(c => c.UtcNow == Now);
        _sut = new ContactService(_uow.Object, _mail.Object, new ConfigurationBuilder().Build(), _journal.Logger(clock), turnstile.Object, throttle.Object, clock);
    }

    private static SubmitContactDto Submission() => new() { Name = "Okur", Email = "okur@ornek.test", Message = "Merhaba", TurnstileToken = "jeton" };

    [Fact]
    public async Task Mesaj_kaydi_iki_postanin_sonucundan_sonra_tek_satir_yazilir()
    {
        var result = await _sut.SubmitAsync(Submission(), null, null);

        result.Success.Should().BeTrue();
        _journal.Events.Should().Equal("mail", "mail", "log:Information");
        _journal.Logs[0].Message.Should().Contain("Id: 5")
            .And.Contain("Site sahibine bildirim gönderildi")
            .And.Contain("Gönderene yanıt gönderildi");
    }

    [Fact]
    public async Task Postalardan_biri_gonderilemezse_kayit_hata_seviyesinde_yazilir()
    {
        _outcomes[MailTemplateDefinitions.ContactUser] = Result.Fail("Posta gönderilemedi.", "SMTP hatası (contact-user): 535", 502);

        var result = await _sut.SubmitAsync(Submission(), null, null);

        result.Success.Should().BeTrue("form yanıtı posta kutusuna değil kaydedilmiş mesaja göre verilir");
        _journal.Events.Should().Equal("mail", "mail", "log:Error");
        _journal.Logs[0].Message.Should().Contain("Site sahibine bildirim gönderildi")
            .And.Contain("Gönderene yanıt gönderilemedi")
            .And.Contain("535");
    }
}
