using FurkanTural_Admin.Models.MailTemplate;

namespace FurkanTural_Admin.Services;

public interface IMailTemplateApiClient
{
    Task<IReadOnlyList<MailTemplateAdminDto>> GetAllForAdminAsync(string token, CancellationToken ct = default);
    Task<IReadOnlyList<MailTemplateTypeOptionDto>> GetTypesAsync(string token, CancellationToken ct = default);
    Task<IReadOnlyList<AppSourceOptionDto>> GetAppSourcesAsync(string token, CancellationToken ct = default);
    Task<string?> GetHtmlContentAsync(int id, string token, CancellationToken ct = default);
    Task<bool> CreateAsync(MailTemplateFormDto dto, string token, CancellationToken ct = default);
    Task<bool> UpdateAsync(int id, MailTemplateFormDto dto, string token, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, string token, CancellationToken ct = default);
    Task<bool> ToggleActiveAsync(int id, string token, CancellationToken ct = default);
    Task<bool> RestoreAsync(int id, string token, CancellationToken ct = default);
}