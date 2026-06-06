using FurkanTural_Admin.Models.Contact;

namespace FurkanTural_Admin.Services;

public interface IContactApiClient
{
    Task<IReadOnlyList<ContactAdminDto>> GetAllForAdminAsync(string token, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, string token, CancellationToken ct = default);
    Task<bool> ToggleActiveAsync(int id, string token, CancellationToken ct = default);
    Task<bool> RestoreAsync(int id, string token, CancellationToken ct = default);
    Task<bool> MarkAsReadAsync(int id, string token, CancellationToken ct = default);
}
