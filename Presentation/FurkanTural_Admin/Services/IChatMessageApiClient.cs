using FurkanTural_Admin.Models.Common;
using FurkanTural_Admin.Helpers;
using FurkanTural_Admin.Models.ChatMessage;

namespace FurkanTural_Admin.Services;

public interface IChatMessageApiClient
{
    Task<IReadOnlyList<ChatMessageAdminDto>> GetAllForAdminAsync(string token, CancellationToken ct = default);
    Task<(IReadOnlyList<ChatMessageAdminDto> Rows, int TotalFiltered)> GetAdminPagedAsync(AdminListRequest request, string token, CancellationToken ct = default);
    Task<StatusCountsModel?> GetAdminCountsAsync(AdminListRequest request, string token, CancellationToken ct = default);
    Task<bool> ToggleActiveAsync(int id, string token, CancellationToken ct = default);
    Task<bool> RestoreAsync(int id, string token, CancellationToken ct = default);

    /// <summary>Sohbet ekini (ses/foto/video) API'nin yetkili ucundan akış olarak getirir. Chat ekleri API'de statik sunulmadığından admin önizlemesi bu proxy ile çalışır. Başarısızlıkta (null, null) döner.</summary>
    Task<(Stream? Stream, string? ContentType)> GetAttachmentAsync(string file, string token, CancellationToken ct = default);
}
