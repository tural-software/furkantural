using System.Text.Json;
using FluentAssertions;
using FurkanTural_API.Controllers.Base;
using FurkanTural_Application.Wrappers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FurkanTural_API.Tests;

public class BaseApiControllerTests
{
    private sealed class TestController : BaseApiController
    {
        public IActionResult Expose(Result result) => ToActionResult(result);
        public IActionResult Expose<T>(Result<T> result) => ToActionResult(result);
    }

    private sealed class CaptureLogger(List<string> sink) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel level, EventId id, TState state, Exception? ex, Func<TState, Exception?, string> formatter)
            => sink.Add($"{level}|{formatter(state, ex)}");
    }

    private sealed class CaptureProvider(List<string> sink) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new CaptureLogger(sink);
        public void Dispose() { }
    }

    private static (TestController Controller, List<string> Logs) Build()
    {
        var logs = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(_ => LoggerFactory.Create(b => b.AddProvider(new CaptureProvider(logs))));

        var http = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
        http.Request.Method = "POST";
        http.Request.Path = "/api/v1/Auth/register";

        var controller = new TestController { ControllerContext = new ControllerContext { HttpContext = http } };
        return (controller, logs);
    }

    [Fact]
    public void Basarisiz_zarfin_ic_mesaji_gunluge_dusar()
    {
        var (controller, logs) = Build();

        controller.Expose(Result.Fail("Bu kullanıcı adı zaten kullanılıyor.", "Kayıt reddedildi: #7 silinmiş hesap."));

        logs.Should().ContainSingle();
        logs[0].Should().Contain("Kayıt reddedildi: #7 silinmiş hesap.");
        logs[0].Should().StartWith("Information|");
    }

    [Fact]
    public void Ic_mesaj_istemciye_cikmaz()
    {
        var (controller, _) = Build();

        var action = (ObjectResult)controller.Expose(Result.Fail("DISARI", "ICERIDE"));
        var body = JsonSerializer.Serialize(action.Value, action.Value!.GetType());

        body.Should().NotContain("ICERIDE");
        body.Should().NotContain("InternalMessage");
        body.Should().Contain("DISARI");
    }

    [Fact]
    public void Ic_mesaj_yoksa_gunluge_hicbir_sey_yazilmaz()
    {
        var (controller, logs) = Build();

        controller.Expose(Result.Fail("Yalnızca dışarıya dönen metin."));

        logs.Should().BeEmpty();
    }

    [Fact]
    public void Basarili_zarf_gunluge_yazilmaz()
    {
        var (controller, logs) = Build();

        controller.Expose(Result.Ok("tamam"));

        logs.Should().BeEmpty();
    }

    [Fact]
    public void Durum_kodu_zarftaki_ile_ayni_kalir()
    {
        var (controller, _) = Build();

        var action = (ObjectResult)controller.Expose(Result.Fail("çakışma", "iç", 409));

        action.StatusCode.Should().Be(409);
        ((Result)action.Value!).StatusCode.Should().Be(409);
    }

    [Fact]
    public void Sayfali_zarf_turunu_kaybetmez()
    {
        var (controller, _) = Build();

        var action = (ObjectResult)controller.Expose(PagedResult<string>.Ok(["a", "b"], 2, 1, 10));

        action.Value.Should().BeOfType<PagedResult<string>>();
    }
}
