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
}
