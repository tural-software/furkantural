using FurkanTural_Admin.Models.Report;

namespace FurkanTural_Admin.Services;

public interface IReportApiClient
{
    Task<IReadOnlyList<ReportAdminDto>> GetAllForAdminAsync(string token, CancellationToken ct = default);
    Task<bool> UpdateStatusAsync(int id, string status, string? adminNote, string token, CancellationToken ct = default);
    Task<bool> ToggleActiveAsync(int id, string token, CancellationToken ct = default);
    Task<bool> RestoreAsync(int id, string token, CancellationToken ct = default);
}