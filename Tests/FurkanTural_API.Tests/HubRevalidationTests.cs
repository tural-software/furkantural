using System.Security.Claims;
using FluentAssertions;
using FurkanTural_API.Hubs;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Domain.Constants;
using FurkanTural_Domain.Entities;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace FurkanTural_API.Tests;

/// <summary>Açık bir WebSocket yalnızca kurulurken doğrulanıyordu. Yasaklanan, silinen ya da rolü düşürülen kullanıcı açık sekmesinden mesaj atmaya ve arama başlatmaya devam edebiliyordu.</summary>
public class HubRevalidationTests
{
    private const string SolutionMarker = "FurkanTural.slnx";

    private sealed class Connection(ClaimsPrincipal user) : HubCallerContext
    {
        public bool Aborted { get; private set; }
        public override string ConnectionId => "baglanti-1";
        public override string? UserIdentifier => "7";
        public override ClaimsPrincipal? User => user;
        public override IDictionary<object, object?> Items { get; } = new Dictionary<object, object?>();
        public override Microsoft.AspNetCore.Http.Features.IFeatureCollection Features { get; } = new Microsoft.AspNetCore.Http.Features.FeatureCollection();
        public override CancellationToken ConnectionAborted => CancellationToken.None;
        public override void Abort() => Aborted = true;
    }

    private sealed class DummyHub : Hub
    {
        public Task Ping() => Task.CompletedTask;
    }

    private static ClaimsPrincipal Token(string? stamp)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "7"), new(ClaimTypes.Role, "User") };
        if (stamp is not null)
            claims.Add(new Claim(ClaimDefinitions.SecurityStamp, stamp));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
    }

    private static IServiceProvider Services(User? account)
    {
        var users = new Mock<IUserRepository>();
        users.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(account);
        var uow = new Mock<IUnitOfWork>();
        uow.SetupGet(u => u.Users).Returns(users.Object);
        return new ServiceCollection().AddSingleton(uow.Object).BuildServiceProvider();
    }

    private static async Task<(bool Called, bool Aborted, Exception? Error)> Invoke(ClaimsPrincipal user, User? account)
    {
        var connection = new Connection(user);
        var hub = new DummyHub();
        var context = new HubInvocationContext(connection, Services(account), hub, typeof(DummyHub).GetMethod(nameof(DummyHub.Ping))!, []);
        var called = false;

        try
        {
            await new SecurityStampHubFilter().InvokeMethodAsync(context, _ => { called = true; return ValueTask.FromResult<object?>(null); });
            return (called, connection.Aborted, null);
        }
        catch (Exception ex)
        {
            return (called, connection.Aborted, ex);
        }
    }

    [Fact]
    public async Task Damgasi_gecerli_kullanicinin_cagrisi_calisir()
    {
        var result = await Invoke(Token("damga"), new User { Id = 7, SecurityStamp = "damga" });

        result.Called.Should().BeTrue();
        result.Aborted.Should().BeFalse();
    }

    [Theory]
    [InlineData("eski-damga", true)]
    [InlineData(null, true)]
    [InlineData("damga", false)]
    public async Task Gecersiz_oturumun_cagrisi_calismaz_ve_baglanti_kapanir(string? tokenStamp, bool accountExists)
    {
        var account = accountExists ? new User { Id = 7, SecurityStamp = "damga" } : null;

        var result = await Invoke(Token(tokenStamp), account);

        result.Called.Should().BeFalse("yasaklanan ya da silinen kullanıcının açık sekmesi mesaj atmaya devam etmemeli");
        result.Aborted.Should().BeTrue();
        result.Error.Should().BeOfType<HubException>();
    }

    [Fact]
    public void Hublar_suzgecle_ve_jeton_bitisinde_kapanacak_sekilde_kaydedilir()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, SolutionMarker)))
            directory = directory.Parent;

        var program = File.ReadAllText(Path.Combine(directory!.FullName, "Web", "FurkanTural_API", "Program.cs"));

        program.Should().Contain("AddFilter<SecurityStampHubFilter>()");
        program.Should().Contain("app.MapHub<ChatHub>(\"/hubs/chat\", o => o.CloseOnAuthenticationExpiration = true);");
        program.Should().Contain("app.MapHub<AdminHub>(\"/hubs/admin\", o => o.CloseOnAuthenticationExpiration = true);");
        program.Should().NotContain("access_token",
            "jeton sorgu dizesinde taşınırsa sunucu ve vekil günlüklerine düşer; web istemcileri BFF üzerinden başlıkla bağlanıyor");
    }
}
