using FurkanTural_Portfolio.Models;

namespace FurkanTural_Portfolio.Services;

public interface IPortfolioApiService
{
    Task<IReadOnlyList<SkillViewModel>> GetSkillsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ProjectViewModel>> GetProjectsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SongViewModel>> GetSongsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ExperienceViewModel>> GetExperiencesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<EducationViewModel>> GetEducationsAsync(CancellationToken ct = default);

    Task<ProjectViewModel?> GetProjectByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<RemoteImageViewModel>> GetProjectImagesAsync(int projectId, CancellationToken ct = default);
}
