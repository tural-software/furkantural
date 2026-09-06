using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.DTOs.Music;
using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

public interface IMusicService : IService<MusicDto, CreateMusicDto, UpdateMusicDto>, IBulkService
{
    Task<Result<IEnumerable<AdminMusicDto>>> GetAllForAdminAsync(CancellationToken cancellationToken = default);
    Task<Result<AdminMusicDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<AdminMusicDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<AdminMusicDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<AdminMusicDto>> GetAllForAdminPagedAsync(AdminListQuery query, string? artist, int? musicId, CancellationToken cancellationToken = default);
    Task<Result<AdminStatusCountsDto>> GetAdminStatusCountsAsync(AdminListQuery query, string? artist, int? musicId, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<AdminOptionDto>>> GetAdminOptionsAsync(string? search, int? take, CancellationToken cancellationToken = default);
}