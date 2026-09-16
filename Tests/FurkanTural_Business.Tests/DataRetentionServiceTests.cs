using FluentAssertions;
using FurkanTural_Application.DTOs.Log;
using FurkanTural_Application.DTOs.Retention;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using FurkanTural_Business.Helpers;
using FurkanTural_Business.Services.Concrete;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FurkanTural_Business.Tests;

/// <summary>Aylık saklama temizliği. Aydınlatma metinleri 2 yıl saklama ve en geç altı ayda bir imha söz verir; temizlik ayda bir elle yapılır ve sınırı 23 aydır.</summary>
public class DataRetentionServiceTests
{
    private static readonly DateTime Now = new(2026, 9, 16, 12, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IDataRetentionStore> _store = new();
    private readonly Mock<IFileService> _files = new();
    private readonly List<CreateLogDto> _logged = [];
    private readonly List<string> _order = [];

    private DataRetentionService Build()
    {
        var logService = new Mock<ILogService>();
        logService.Setup(l => l.CreateAsync(It.IsAny<CreateLogDto>(), It.IsAny<CancellationToken>()))
            .Callback<CreateLogDto, CancellationToken>((dto, _) => _logged.Add(dto))
            .ReturnsAsync(Result<LogDto>.Ok(new LogDto()));
        var clock = Mock.Of<IClock>(c => c.UtcNow == Now);

        return new DataRetentionService(_store.Object, _files.Object,
            new ActivityLogger(logService.Object, Mock.Of<IHttpContextAccessor>(), clock), clock,
            NullLogger<DataRetentionService>.Instance);
    }

    [Fact]
    public async Task Sinir_yirmi_uc_ay_oncesidir()
    {
        _store.Setup(s => s.CountAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new DataRetentionCategoryDto("logs", "Kayıtlar", 4)]);

        var result = await Build().PreviewAsync();

        result.Data!.Cutoff.Should().Be(Now.AddMonths(-23));
        result.Data.Total.Should().Be(4);
        _store.Verify(s => s.CountAsync(Now.AddMonths(-23), Now.AddDays(-30), It.IsAny<CancellationToken>()), Times.Once,
            "bildirim abonelikleri Chatural metnindeki 30 günlük eşiğe göre silinir");
        _store.Verify(s => s.PurgeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never,
            "önizleme hiçbir şey silmemeli");
    }

    [Fact]
    public async Task Dosyalar_satirlar_silindikten_sonra_kaldirilir()
    {
        _store.Setup(s => s.PurgeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Callback(() => _order.Add("satirlar"))
            .ReturnsAsync(new DataRetentionPurge([new DataRetentionCategoryDto("messages", "Mesajlar", 2)],
                ["chats/images/a.png", "chats/images/a.png", "users/images/b.png", ""]));
        _files.Setup(f => f.DeleteAsync(It.IsAny<string?>()))
            .Callback<string?>(f => _order.Add(f!))
            .Returns(Task.CompletedTask);

        var result = await Build().PurgeAsync(1);

        _order.First().Should().Be("satirlar", "dosya önce silinip veri tabanı işlemi geri alınsaydı kayıt dururken dosyası gitmiş olurdu");
        _files.Verify(f => f.DeleteAsync("chats/images/a.png"), Times.Once);
        _files.Verify(f => f.DeleteAsync("users/images/b.png"), Times.Once);
        _files.Verify(f => f.DeleteAsync(""), Times.Never);
        result.Data!.FilesDeleted.Should().Be(2);
        result.Data.Total.Should().Be(2);
    }

    [Fact]
    public async Task Silinemeyen_dosya_islemi_durdurmaz_ve_kayda_duser()
    {
        _store.Setup(s => s.PurgeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DataRetentionPurge([new DataRetentionCategoryDto("messages", "Mesajlar", 2)], ["bozuk.png", "saglam.png"]));
        _files.Setup(f => f.DeleteAsync("bozuk.png")).ThrowsAsync(new IOException("kilitli"));

        var result = await Build().PurgeAsync(7);

        result.Success.Should().BeTrue();
        result.Data!.FilesFailed.Should().Be(1);
        result.Data.FilesDeleted.Should().Be(1);
        _logged.Should().ContainSingle(l => l.Message!.Contains("silinemeyen dosya: 1") && l.Message.Contains("#7"),
            "temizliği kimin ne zaman yaptığı ve neyin kaldığı denetim kaydında görünmeli");
    }
}
