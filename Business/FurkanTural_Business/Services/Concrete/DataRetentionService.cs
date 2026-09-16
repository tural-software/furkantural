using FurkanTural_Application.DTOs.Retention;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using FurkanTural_Business.Helpers;
using Microsoft.Extensions.Logging;

namespace FurkanTural_Business.Services.Concrete;

/// <summary>Silinen satırların eklerini ve avatarlarını diskten de kaldırır. Dosyalar ancak satırlar kalıcı olarak silindikten sonra silinir: sıra ters olsaydı veri tabanı işlemi geri alındığında kayıt dururken dosyası gitmiş olurdu. Silinemeyen bir dosya işlemi durdurmaz, sayılır ve kayda düşer; o dosyaya artık hiçbir kayıt bakmadığı için sonraki bir temizlikte elle kaldırılabilir.<para>Bildirim aboneliklerinin eşiği saklama süresinden değil Chatural aydınlatma metninden gelir: tarayıcıdan 30 gün girilmezse abonelik silinir.</para></summary>
public class DataRetentionService(
    IDataRetentionStore store,
    IFileService fileService,
    ActivityLogger activityLogger,
    IClock clock,
    ILogger<DataRetentionService> logger) : IDataRetentionService
{
    public const int RetentionMonths = 23;

    public static readonly TimeSpan PushSubscriptionStaleAfter = TimeSpan.FromDays(30);

    private readonly IDataRetentionStore _store = store;
    private readonly IFileService _fileService = fileService;
    private readonly ActivityLogger _activityLogger = activityLogger;
    private readonly IClock _clock = clock;
    private readonly ILogger<DataRetentionService> _logger = logger;

    public async Task<Result<DataRetentionReportDto>> PreviewAsync(CancellationToken cancellationToken = default)
    {
        var (cutoff, staleBefore) = Limits();
        var categories = await _store.CountAsync(cutoff, staleBefore, cancellationToken);
        return Result<DataRetentionReportDto>.Ok(new DataRetentionReportDto { Cutoff = cutoff, Categories = categories });
    }

    public async Task<Result<DataRetentionReportDto>> PurgeAsync(int? userId, CancellationToken cancellationToken = default)
    {
        var (cutoff, staleBefore) = Limits();
        var purge = await _store.PurgeAsync(cutoff, staleBefore, cancellationToken);

        var deleted = 0;
        var failed = 0;
        foreach (var file in purge.Files.Where(f => !string.IsNullOrWhiteSpace(f)).Distinct(StringComparer.Ordinal))
        {
            try
            {
                await _fileService.DeleteAsync(file);
                deleted++;
            }
            catch (Exception ex)
            {
                failed++;
                _logger.LogWarning(ex, "Saklama temizliğinde dosya silinemedi: {File}", file);
            }
        }

        var report = new DataRetentionReportDto
        {
            Cutoff = cutoff,
            Categories = purge.Categories,
            FilesDeleted = deleted,
            FilesFailed = failed
        };

        await _activityLogger.LogAsync(
            $"Saklama süresi dolan veriler silindi. Sınır: {cutoff:yyyy-MM-dd}, silinen kayıt: {report.Total}, " +
            $"silinen dosya: {deleted}, silinemeyen dosya: {failed}, işlemi yapan: #{userId?.ToString() ?? "-"}. " +
            string.Join(", ", purge.Categories.Where(c => c.Count > 0).Select(c => $"{c.Key}={c.Count}")),
            cancellationToken);

        return Result<DataRetentionReportDto>.Ok(report);
    }

    private (DateTime Cutoff, DateTime StaleBefore) Limits()
    {
        var now = _clock.UtcNow;
        return (now.AddMonths(-RetentionMonths), now.Subtract(PushSubscriptionStaleAfter));
    }
}
