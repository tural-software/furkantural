using FluentAssertions;
using FurkanTural_Application.DTOs.Log;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Business.Helpers;
using FurkanTural_Domain.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Moq;

namespace FurkanTural_Business.Tests;

/// <summary>Kayıt kaynağı <c>Uygulama-Bileşen-İşlem</c> biçiminde damgalanır ve tek yerden üretilir. Bileşen isteğin rotasından türer, yani çağrı noktası hiçbir şey söylemese bile satır nereden çıktığını taşır. Bilinmeyen parça yazılmaz: bağlamı olmayan bir çağrı yalnızca uygulama adıyla kalır, uydurma bir bileşen adı almaz.<para>Parçalar temizlenir çünkü bu değerin bir ucu tarayıcıya açıktır: gövdeye ayraç sıkıştırarak kaydı başka bir uygulamanın üstüne yazma denemesi buradan geçemez.</para></summary>
public class LogSourceTests
{
    [Fact]
    public void Parcalar_ayracla_birlesir_bos_olan_yazilmaz()
    {
        LogSources.Compose(LogSources.Api, "Blog", "Create", "Post").Should().Be("FurkanTural_API-Blog-Create-Post");
        LogSources.Compose(LogSources.Api, "Contact", null, "  ").Should().Be("FurkanTural_API-Contact");
        LogSources.Compose(LogSources.Api).Should().Be("FurkanTural_API",
            "bileşen bilgisi yoksa kaynak yalnızca uygulama adıdır; boş parça uydurulmaz");
    }

    [Fact]
    public void Tek_argumanda_gelen_ayrac_da_parcalara_bolunur()
        => LogSources.Compose(LogSources.Admin, "Blog-Insert-Post").Should().Be("FurkanTural_Admin-Blog-Insert-Post",
            "çağıran etiketi tek dize olarak verebilmeli; sonuç iki türlü de aynı olmalı");

    [Fact]
    public void Uygulama_adi_govdeden_uydurulamaz()
    {
        LogSources.Compose(LogSources.Api, "Blog<script>").Should().Be("FurkanTural_API-Blogscript",
            "harf, rakam ve alt çizgi dışındaki her şey atılır");
        LogSources.Compose(LogSources.Api, "x/../FurkanTural_Admin").Should().Be("FurkanTural_API-x-Admin",
            "eğik çizgi de ayraç sayılır; sızan değer ilk parçanın yerini alamaz ve uygulama adına da benzeyemez");
        LogSources.Compose("FurkanTural_Chat", "FurkanTural_Admin-User-Delete").Should().Be("FurkanTural_Chat-Admin-User-Delete",
            "ilk parçadan sonrası uygulama adı taşıyamaz; taşısaydı o adla yapılan arama bu satırı da getirirdi");
        LogSources.Compose(LogSources.Api, "a\nb").Should().Be("FurkanTural_API-ab",
            "satır sonu düz metin günlükte sahte satır üretebilirdi");
    }

    [Fact]
    public void Uzun_deger_kolon_genisligine_kirpilir()
        => LogSources.Compose(LogSources.Api, new string('x', 500)).Length.Should().Be(LogSources.MaxLength,
            "kolon 200 karakter; kırpılmazsa kaydetme anında hata doğar ve günlük satırı hiç yazılmaz");

    [Theory]
    [InlineData("Chat", "FurkanTural_Chat")]
    [InlineData("Portfolio", "FurkanTural_Portfolio")]
    [InlineData(AppSourceDefinitions.Admin, "FurkanTural_Admin")]
    public void Claim_kodu_uygulama_adina_cevrilir(string code, string expected)
        => LogSources.ForApp(code).Should().Be(expected);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Bos_claim_uygulama_adi_uretmez(string? code)
        => LogSources.ForApp(code).Should().BeNull("kaynağı bilinmeyen bir kaydı bilinen bir uygulamaya yazmak yanlış iz bırakır");

    [Fact]
    public void Bilesen_isteğin_rotasindan_turer()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.GetRouteData().Values["controller"] = "Blog";
        context.GetRouteData().Values["action"] = "Create";

        LogSourceBuilder.FromContext(context, LogSources.Api).Should().Be("FurkanTural_API-Blog-Create-Post",
            "doksan yedi çağrı noktası kendi etiketini yazmaz; rota zaten biliyor");
    }

    [Fact]
    public void Etiket_verilirse_rota_yok_sayilir()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.GetRouteData().Values["controller"] = "Blog";
        context.GetRouteData().Values["action"] = "Bulk";

        LogSourceBuilder.FromContext(context, LogSources.Api, "Blog-BulkDelete").Should().Be("FurkanTural_API-Blog-BulkDelete",
            "tek uçtan birden çok iş yürüten çağrılar kendi anlamını verebilmeli");
    }

    [Fact]
    public void Baglam_yoksa_yalnizca_uygulama_adi_kalir()
        => LogSourceBuilder.FromContext(null, LogSources.Api).Should().Be("FurkanTural_API",
            "arka plan çağrısında bileşen bilgisi gerçekten yoktur; olmayanı yazmaktansa boş bırakılır");

    [Fact]
    public async Task ActivityLogger_kaydi_rotadan_damgalar()
    {
        CreateLogDto? written = null;
        var logService = new Mock<ILogService>();
        logService.Setup(s => s.CreateAsync(It.IsAny<CreateLogDto>(), It.IsAny<CancellationToken>()))
            .Callback<CreateLogDto, CancellationToken>((d, _) => written = d)
            .ReturnsAsync(FurkanTural_Application.Wrappers.Result<LogDto>.Ok(new LogDto()));

        var context = new DefaultHttpContext();
        context.Request.Method = "PUT";
        context.GetRouteData().Values["controller"] = "Skill";
        context.GetRouteData().Values["action"] = "Update";
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(a => a.HttpContext).Returns(context);

        var sut = new ActivityLogger(logService.Object, accessor.Object, Mock.Of<IClock>());
        await sut.LogAsync("Yetenek güncellendi. Id: 3");

        written!.Source.Should().Be("FurkanTural_API-Skill-Update-Put");
        written.Level.Should().Be("Information");
    }
}
