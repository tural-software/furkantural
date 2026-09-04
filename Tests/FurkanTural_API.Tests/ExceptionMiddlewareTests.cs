using Moq;
using Microsoft.Extensions.Logging;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.DTOs.Log;
using System.Text.Json;
using FluentAssertions;
using FurkanTural_API.Middlewares;
using FurkanTural_Application.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace FurkanTural_API.Tests;

public class ExceptionMiddlewareTests
{
    private const string DuplicateMessage =
        "Cannot insert duplicate key row in object 'dbo.Users' with unique index 'IX_Users_Username'.";

    private static async Task<(int Status, string Body)> InvokeWith(Exception thrown)
    {
        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().BuildServiceProvider()
        };
        context.Request.Path = "/api/v1/Auth/register";
        context.Response.Body = new MemoryStream();

        var middleware = new ExceptionMiddleware(_ => throw thrown, NullLogger<ExceptionMiddleware>.Instance);
        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        return (context.Response.StatusCode, body);
    }

    private static string SingleError(string body)
        => JsonDocument.Parse(body).RootElement.GetProperty("errors")[0].GetString()!;

    [Fact]
    public async Task Tekil_ihlali_500_degil_409_doner()
    {
        var (status, body) = await InvokeWith(new DuplicateEntityException("IX_Users_Username", new Exception()));

        status.Should().Be(409);
        SingleError(body).Should().Be("Bu kayıt zaten var.");
    }

    [Fact]
    public async Task Yabanci_anahtar_ihlali_409_doner()
    {
        var (status, body) = await InvokeWith(new RelatedEntityMissingException("FK_A_B", new Exception()));

        status.Should().Be(409);
        SingleError(body).Should().Be("İlişkili kayıt bulunamadı.");
    }

    [Fact]
    public async Task Cekisme_disindaki_istisna_500_kalir()
    {
        var (status, body) = await InvokeWith(new InvalidOperationException("beklenmedik"));

        status.Should().Be(500);
        SingleError(body).Should().Be("Sunucu tarafında beklenmeyen bir hata oluştu.");
    }

    [Fact]
    public async Task Kisit_adi_istemciye_sizmaz()
    {
        var (_, body) = await InvokeWith(new DuplicateEntityException("IX_Users_Username", new Exception(DuplicateMessage)));

        body.Should().NotContain("IX_Users_Username");
        body.Should().NotContain("dbo.Users");
    }

    [Fact]
    public async Task Yanit_zarfi_Result_ile_ayni_alanlari_tasir()
    {
        var (_, body) = await InvokeWith(new DuplicateEntityException(null, new Exception()));

        var root = JsonDocument.Parse(body).RootElement;
        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.GetProperty("statusCode").GetInt32().Should().Be(409);
        root.GetProperty("errors").GetArrayLength().Should().Be(1);
    }

    private sealed class ListLogger : ILogger<ExceptionMiddleware>
    {
        public List<LogLevel> Levels { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Levels.Add(logLevel);
    }

    private static async Task<(int Status, string Body, ListLogger Logger, Mock<ILogService> Log)> InvokeCancelled(bool clientAborted)
    {
        var log = new Mock<ILogService>();
        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddSingleton(log.Object).BuildServiceProvider(),
            RequestAborted = new CancellationToken(canceled: clientAborted)
        };
        context.Request.Path = "/api/v1/friend/me";
        context.Response.Body = new MemoryStream();

        var logger = new ListLogger();
        var middleware = new ExceptionMiddleware(_ => throw new TaskCanceledException("A task was canceled."), logger);
        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        return (context.Response.StatusCode, body, logger, log);
    }

    [Fact]
    public async Task Istemci_vazgecince_499_doner_ve_hicbir_yere_kayit_dusmez()
    {
        var (status, body, logger, log) = await InvokeCancelled(clientAborted: true);

        status.Should().Be(499, "isteği istemci kesti; sunucu arızası değil");
        body.Should().BeEmpty("giden kimse yokken gövde yazmak boşa iş");
        logger.Levels.Should().NotContain(LogLevel.Error, "sekme kapatan kullanıcı günlükte arıza gibi görünmemeli");
        log.Verify(l => l.CreateAsync(It.IsAny<CreateLogDto>(), It.IsAny<CancellationToken>()), Times.Never,
            "Logs tablosu gerçek arızalar içindir; her vazgeçen istemci bir satır yazdırırsa tablo gürültüye boğulur");
    }

    [Fact]
    public async Task Istek_canliyken_gelen_iptal_500_kalir_ve_kaydedilir()
    {
        var (status, _, logger, log) = await InvokeCancelled(clientAborted: false);

        status.Should().Be(500, "istemci beklerken içeride biten bir iptal gerçek bir zaman aşımıdır");
        logger.Levels.Should().Contain(LogLevel.Error);
        log.Verify(l => l.CreateAsync(It.IsAny<CreateLogDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
