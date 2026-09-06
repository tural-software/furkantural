using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Wrappers;
using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Business.Helpers;

/// <summary>Toplu işlemin tek gövdesi; her servis kendi deposunu vererek buraya devreder. Kimlikler tekilleştirilir, uygun durumdaki satırlar işlenir ve tamamı tek SaveChangesAsync ile kaydedilir. Hiçbir satır değişmezse veri tabanına yazma yapılmaz.<para>Tavan sayfa boyu tavanıyla aynıdır: bir sayfada seçilebilecekten fazlası tek istekte işlenmez.</para></summary>
public static class BulkActions
{
    public const int MaxBulk = 100;

    public static async Task<Result<BulkActionResultDto>> ApplyAsync<T>(
        IUnitOfWork unitOfWork,
        IRepository<T> repository,
        BulkAction action,
        IReadOnlyCollection<int> ids,
        int? userId,
        string entityName,
        ActivityLogger? activityLogger = null,
        CancellationToken cancellationToken = default) where T : BaseEntity
    {
        var wanted = ids.Where(i => i > 0).Distinct().ToList();
        if (wanted.Count == 0)
            return Result<BulkActionResultDto>.Fail("En az bir kayıt seçilmeli.", statusCode: 400);
        if (wanted.Count > MaxBulk)
            return Result<BulkActionResultDto>.Fail($"Tek istekte en çok {MaxBulk} kayıt işlenir.", statusCode: 400);

        var rows = (await repository.GetAllForAdminAsync(x => wanted.Contains(x.Id), cancellationToken)).ToList();
        var affected = new List<int>();
        foreach (var row in rows)
        {
            if (!IsEligible(row, action)) continue;

            switch (action)
            {
                case BulkAction.Delete:
                    await repository.SoftDeleteAsync(row, userId, cancellationToken);
                    break;
                case BulkAction.Restore:
                    row.UpdatedBy = userId;
                    await repository.RestoreAsync(row, cancellationToken);
                    break;
                default:
                    row.IsActive = action == BulkAction.Activate;
                    row.UpdatedBy = userId;
                    await repository.UpdateAsync(row, cancellationToken);
                    break;
            }
            affected.Add(row.Id);
        }

        if (affected.Count > 0)
            await unitOfWork.SaveChangesAsync(cancellationToken);

        var skipped = wanted.Except(affected).ToList();
        if (activityLogger is not null)
            await activityLogger.LogAsync($"Toplu {entityName} işlemi: {action}. Etkilenen: {affected.Count}, atlanan: {skipped.Count}.", cancellationToken);

        return Result<BulkActionResultDto>.Ok(new BulkActionResultDto(wanted.Count, affected.Count, skipped));
    }

    private static bool IsEligible(BaseEntity row, BulkAction action) => action switch
    {
        BulkAction.Delete => !row.IsDeleted,
        BulkAction.Restore => row.IsDeleted,
        BulkAction.Activate => !row.IsDeleted && !row.IsActive,
        BulkAction.Deactivate => !row.IsDeleted && row.IsActive,
        _ => false
    };
}
