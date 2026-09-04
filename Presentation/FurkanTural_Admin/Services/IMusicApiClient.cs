using FurkanTural_Admin.Models.Common;
using FurkanTural_Admin.Helpers;
using FurkanTural_Admin.Models.Music;

namespace FurkanTural_Admin.Services;

public interface IMusicApiClient
{
    Task<IReadOnlyList<MusicAdminDto>> GetAllForAdminAsync(string token, CancellationToken ct = default);
    Task<(IReadOnlyList<MusicAdminDto> Rows, int TotalFiltered)> GetAdminPagedAsync(AdminListRequest request, string token, CancellationToken ct = default);
    Task<StatusCountsModel?> GetAdminCountsAsync(AdminListRequest request, string token, CancellationToken ct = default);
    Task<bool> CreateAsync(MusicFormDto dto, string token, CancellationToken ct = default);
    Task<bool> UpdateAsync(int id, MusicFormDto dto, string token, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, string token, CancellationToken ct = default);
    Task<bool> ToggleActiveAsync(int id, string token, CancellationToken ct = default);
    Task<bool> RestoreAsync(int id, string token, CancellationToken ct = default);
}