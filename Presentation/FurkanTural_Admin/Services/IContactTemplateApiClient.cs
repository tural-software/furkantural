using FurkanTural_Admin.Models.ContactTemplate;

namespace FurkanTural_Admin.Services;

public interface IContactTemplateApiClient
{
    Task<IReadOnlyList<ContactTemplateAdminDto>> GetAllForAdminAsync(string token, CancellationToken ct = default);
    Task<string?> GetHtmlContentAsync(int id, string token, CancellationToken ct = default);
    Task<bool> CreateAsync(ContactTemplateFormDto dto, string token, CancellationToken ct = default);
    Task<bool> UpdateAsync(int id, ContactTemplateFormDto dto, string token, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, string token, CancellationToken ct = default);
    Task<bool> ToggleActiveAsync(int id, string token, CancellationToken ct = default);
    Task<bool> RestoreAsync(int id, string token, CancellationToken ct = default);
}