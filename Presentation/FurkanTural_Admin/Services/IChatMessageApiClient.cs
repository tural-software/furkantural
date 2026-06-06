using FurkanTural_Admin.Models.ChatMessage;

namespace FurkanTural_Admin.Services;

public interface IChatMessageApiClient
{
    Task<IReadOnlyList<ChatMessageAdminDto>> GetAllForAdminAsync(string token, CancellationToken ct = default);
    Task<bool> ToggleActiveAsync(int id, string token, CancellationToken ct = default);
    Task<bool> RestoreAsync(int id, string token, CancellationToken ct = default);
}
