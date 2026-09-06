using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.DTOs.MusicImage;
using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

public interface IMusicImageService : IService<MusicImageDto, CreateMusicImageDto, UpdateMusicImageDto>, IBulkService
{
    Task<Result<IEnumerable<MusicImageDto>>> GetByMusicIdAsync(int musicId, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<AdminMusicImageDto>>> GetAllForAdminAsync(CancellationToken cancellationToken = default);
    Task<Result<AdminMusicImageDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<AdminMusicImageDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<AdminMusicImageDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<AdminMusicImageDto>> GetAllForAdminPagedAsync(AdminListQuery query, bool? isCover, int? musicId, CancellationToken cancellationToken = default);
    Task<Result<AdminStatusCountsDto>> GetAdminStatusCountsAsync(AdminListQuery query, bool? isCover, int? musicId, CancellationToken cancellationToken = default);
}