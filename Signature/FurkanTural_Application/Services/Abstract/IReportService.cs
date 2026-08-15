using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.DTOs.Report;
using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

/// <summary>
/// Kullanıcı şikayetleri. Hedef türü ve durum serbest metin gibi görünse de
/// <see cref="FurkanTural_Domain.Constants.ReportDefinitions"/> ile doğrulanır, listede olmayan değer
/// reddedilir. Şikayet edilen kaydın kendisi TargetId ile tutulur ve bu alanın foreign key'i yoktur;
/// hangi tabloya baktığını yalnızca hedef türü söyler. UpdateStatusAsync'e boş not gönderilirse
/// mevcut not korunur, üzerine boş yazılmaz.
/// </summary>
public interface IReportService
{
    Task<Result> CreateAsync(int reporterId, CreateReportDto dto, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<AdminReportDto>>> GetAllForAdminAsync(CancellationToken cancellationToken = default);
    Task<Result<AdminReportDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<AdminReportDto>> UpdateStatusAsync(int id, string status, string? adminNote, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<AdminReportDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<AdminReportDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default);
}