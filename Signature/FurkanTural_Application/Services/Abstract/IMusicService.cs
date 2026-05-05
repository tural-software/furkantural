using FurkanTural_Application.DTOs.Music;
using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

public interface IMusicService : IService<MusicDto, CreateMusicDto, UpdateMusicDto>
{
    Task<Result<IEnumerable<AdminMusicDto>>> GetAllForAdminAsync(CancellationToken cancellationToken = default);
    Task<Result<AdminMusicDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<AdminMusicDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<AdminMusicDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
}