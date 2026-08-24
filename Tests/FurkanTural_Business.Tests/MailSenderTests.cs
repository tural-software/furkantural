using System.Linq.Expressions;
using FluentAssertions;
using FurkanTural_Application.DTOs.Mail;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Business.Services.Concrete;
using FurkanTural_Domain.Constants;
using FurkanTural_Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FurkanTural_Business.Tests;

public class MailSenderTests
{
    private const int ActivationTypeId = 3;
    private const int ChatId = 3;
    private const int PortfolioId = 1;

    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IRepository<MailTemplateType>> _types = new();
    private readonly Mock<IRepository<AppSource>> _appSources = new();
    private readonly Mock<IRepository<MailTemplate>> _templates = new();
    private readonly Mock<IEmailService> _email = new();

    private readonly List<MailTemplateType> _typeRows =
    [
        new() { Id = ActivationTypeId, Code = MailTemplateDefinitions.AccountActivation }
    ];

    private readonly List<AppSource> _appSourceRows =
    [
        new() { Id = PortfolioId, Code = AppSourceDefinitions.Portfolio },
        new() { Id = ChatId, Code = AppSourceDefinitions.Chat }
    ];

    private readonly List<MailTemplate> _templateRows = [];
    private readonly List<(string Subject, string Body)> _delivered = [];

    private readonly MailSender _sut;

    public MailSenderTests()
    {
        _types.Setup(r => r.GetAsync(It.IsAny<Expression<Func<MailTemplateType, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<MailTemplateType, bool>> p, CancellationToken _) => _typeRows.FirstOrDefault(p.Compile()));

        _appSources.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AppSource, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<AppSource, bool>> p, CancellationToken _) => _appSourceRows.FirstOrDefault(p.Compile()));

        _templates.Setup(r => r.GetAsync(It.IsAny<Expression<Func<MailTemplate, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<MailTemplate, bool>> p, CancellationToken _) => _templateRows.FirstOrDefault(p.Compile()));

        _email.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((_, s, b, _) => _delivered.Add((s, b)))
            .Returns(Task.CompletedTask);

        _uow.SetupGet(u => u.MailTemplateTypes).Returns(_types.Object);
        _uow.SetupGet(u => u.AppSources).Returns(_appSources.Object);
        _uow.SetupGet(u => u.MailTemplates).Returns(_templates.Object);

        _sut = new MailSender(_uow.Object, new MailRenderer(NullLogger<MailRenderer>.Instance), _email.Object);
    }

    private void TemplateFor(int? appSourceId, string marker)
        => _templateRows.Add(new MailTemplate
        {
            Id = _templateRows.Count + 1,
            MailTemplateTypeId = ActivationTypeId,
            AppSourceId = appSourceId,
            Subject = $"{marker} - {{{{DisplayName}}}}",
            HtmlContent = $"<p>{marker} {{{{ActivationUrl}}}}</p>"
        });

    private Task<FurkanTural_Application.Wrappers.Result> Send(string? appSourceCode)
        => _sut.SendAsync(MailTemplateDefinitions.AccountActivation, appSourceCode, "deneme@ornek.test",
            new AccountActivationMailDto { DisplayName = "Ada", ActivationUrl = "https://ornek.test/x" });

    [Fact]
    public async Task Projenin_kendi_sablonu_varsa_o_kullanilir()
    {
        TemplateFor(null, "GENEL");
        TemplateFor(ChatId, "CHATURAL");

        var result = await Send(AppSourceDefinitions.Chat);

        result.Success.Should().BeTrue();
        _delivered[0].Body.Should().Contain("CHATURAL").And.NotContain("GENEL");
    }

    [Fact]
    public async Task Projenin_sablonu_yoksa_genel_surume_dusulur()
    {
        TemplateFor(null, "GENEL");
        TemplateFor(ChatId, "CHATURAL");

        var result = await Send(AppSourceDefinitions.Portfolio);

        result.Success.Should().BeTrue();
        _delivered[0].Body.Should().Contain("GENEL");
    }

    [Fact]
    public async Task Taninmayan_proje_adi_hataya_degil_genel_surume_gider()
    {
        TemplateFor(null, "GENEL");

        var result = await Send("BoyleBirProjeYok");

        result.Success.Should().BeTrue("istemcinin bildirdiği bir ad yüzünden posta susmamalı");
        _delivered[0].Body.Should().Contain("GENEL");
    }

    [Fact]
    public async Task Proje_belirtilmezse_genel_surum_kullanilir()
    {
        TemplateFor(null, "GENEL");
        TemplateFor(ChatId, "CHATURAL");

        await Send(null);

        _delivered[0].Body.Should().Contain("GENEL");
    }

    [Fact]
    public async Task Yalnizca_projeye_ozel_sablon_varken_baska_proje_sablonsuz_kalir()
    {
        TemplateFor(ChatId, "CHATURAL");

        var result = await Send(AppSourceDefinitions.Portfolio);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(500);
        _delivered.Should().BeEmpty();
    }

    [Fact]
    public async Task Konu_da_projenin_sablonundan_gelir()
    {
        TemplateFor(null, "GENEL");
        TemplateFor(ChatId, "CHATURAL");

        await Send(AppSourceDefinitions.Chat);

        _delivered[0].Subject.Should().Be("CHATURAL - Ada");
    }

    [Fact]
    public async Task Alici_adresi_bossa_sablon_hic_aranmaz()
    {
        var result = await _sut.SendAsync(MailTemplateDefinitions.AccountActivation, AppSourceDefinitions.Chat, "  ",
            new AccountActivationMailDto());

        result.IsFailure.Should().BeTrue();
        _templates.Verify(r => r.GetAsync(It.IsAny<Expression<Func<MailTemplate, bool>>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Tur_bulunamazsa_gonderim_yapilmaz()
    {
        _typeRows.Clear();
        TemplateFor(ChatId, "CHATURAL");

        var result = await Send(AppSourceDefinitions.Chat);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(500);
        _delivered.Should().BeEmpty();
    }

    [Fact]
    public async Task Govdesi_bos_sablon_gonderilmis_sayilmaz()
    {
        _templateRows.Add(new MailTemplate { MailTemplateTypeId = ActivationTypeId, AppSourceId = ChatId, HtmlContent = "   " });

        var result = await Send(AppSourceDefinitions.Chat);

        result.IsFailure.Should().BeTrue();
        _delivered.Should().BeEmpty();
    }

    [Fact]
    public async Task Smtp_hatasi_istisna_degil_sonuc_olarak_doner()
    {
        TemplateFor(ChatId, "CHATURAL");
        _email.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP kapalı"));

        var result = await Send(AppSourceDefinitions.Chat);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(502);
        result.InternalMessage.Should().Contain("SMTP kapalı");
    }
}
