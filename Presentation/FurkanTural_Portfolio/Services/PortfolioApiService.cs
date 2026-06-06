using System.Net.Http.Json;
using System.Text.Json;
using FurkanTural_Portfolio.Models;
using FurkanTural_Portfolio.Models.Wrappers;

namespace FurkanTural_Portfolio.Services;

public class PortfolioApiService(HttpClient httpClient, ILogger<PortfolioApiService> logger) : IPortfolioApiService
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<PortfolioApiService> _logger = logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<IReadOnlyList<SkillViewModel>> GetSkillsAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<ApiResult<IEnumerable<SkillViewModel>>>("/api/v1/skill", JsonOptions, ct);
            return result?.Data?.ToList().AsReadOnly() ?? (IReadOnlyList<SkillViewModel>)[];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Yetenekler alınamadı.");
            return [];
        }
    }

    public async Task<IReadOnlyList<ProjectViewModel>> GetProjectsAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<ApiResult<IEnumerable<ProjectViewModel>>>("/api/v1/project", JsonOptions, ct);
            return result?.Data?.ToList().AsReadOnly() ?? (IReadOnlyList<ProjectViewModel>)[];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Projeler alınamadı.");
            return [];
        }
    }

    public async Task<IReadOnlyList<SongViewModel>> GetSongsAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<ApiResult<IEnumerable<SongViewModel>>>("/api/v1/music", JsonOptions, ct);
            return result?.Data?.ToList().AsReadOnly() ?? (IReadOnlyList<SongViewModel>)[];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Müzikler alınamadı.");
            return [];
        }
    }

    public async Task<IReadOnlyList<ExperienceViewModel>> GetExperiencesAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<ApiResult<IEnumerable<ExperienceViewModel>>>("/api/v1/experience", JsonOptions, ct);
            return result?.Data?.ToList().AsReadOnly() ?? (IReadOnlyList<ExperienceViewModel>)[];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Deneyimler alınamadı.");
            return [];
        }
    }

    public async Task<IReadOnlyList<EducationViewModel>> GetEducationsAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<ApiResult<IEnumerable<EducationViewModel>>>("/api/v1/education", JsonOptions, ct);
            return result?.Data?.ToList().AsReadOnly() ?? (IReadOnlyList<EducationViewModel>)[];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Eğitimler alınamadı.");
            return [];
        }
    }
}
