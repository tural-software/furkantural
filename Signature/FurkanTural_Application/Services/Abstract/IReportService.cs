using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.DTOs.Report;
using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

public interface IReportService
{
    // ── Üye ──
    Task<Result> CreateAsync(int reporterId, CreateReportDto dto, CancellationToken cancellationToken = default);

    // ── Admin ──
    Task<Result<IEnumerable<AdminReportDto>>> GetAllForAdminAsync(CancellationToken cancellationToken = default);
    Task<Result<AdminReportDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<AdminReportDto>> UpdateStatusAsync(int id, string status, string? adminNote, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<AdminReportDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<AdminReportDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default);
}
