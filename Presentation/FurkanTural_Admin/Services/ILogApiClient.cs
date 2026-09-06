using FurkanTural_Admin.Models.Common;
using FurkanTural_Admin.Models.Log;

namespace FurkanTural_Admin.Services;

public interface ILogApiClient
{
    Task<(IReadOnlyList<LogAdminDto> Rows, int TotalCount)> GetAdminPagedAsync(
        string? level, string? source, string? message,
        DateTime? dateFrom, DateTime? dateTo,
        int pageNumber, int pageSize,
        string token, CancellationToken ct = default);

    Task<EntitySummaryModel?> GetAdminSummaryAsync(string token, CancellationToken ct = default);
}