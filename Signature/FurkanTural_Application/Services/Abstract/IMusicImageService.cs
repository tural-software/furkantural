using FurkanTural_Application.DTOs.MusicImage;
using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

public interface IMusicImageService : IService<MusicImageDto, CreateMusicImageDto, UpdateMusicImageDto>
{
    Task<Result<IEnumerable<MusicImageDto>>> GetByMusicIdAsync(int musicId, CancellationToken cancellationToken = default);
}