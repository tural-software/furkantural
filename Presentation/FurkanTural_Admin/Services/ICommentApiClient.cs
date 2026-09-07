using FurkanTural_Admin.Helpers;
using FurkanTural_Admin.Models.Comment;
using FurkanTural_Admin.Models.Common;

namespace FurkanTural_Admin.Services;

/// <summary>Yorum yönetimi uçları. Yazma uçları <see cref="ApiCallResult"/> döndürür: denetim kararlarının çoğu reddedilebilir — silinmiş yorumun durumu değiştirilemez, bir yanıta yanıt verilemez, yalnızca onaylı yoruma yanıt yazılabilir — ve bu redleri tek bir "işlem başarısız" metnine indirmek, kullanıcıyı sebebi görünmeyen bir hatayla baş başa bırakırdı.<para>Yorumun gövdesini düzenleyen bir uç <b>yoktur</b> ve bu bilinçlidir: başkasının sözünü sessizce değiştirmek yerine karar onaylamak, reddetmek ya da silmektir.</para></summary>
public interface ICommentApiClient
{
    Task<(IReadOnlyList<CommentAdminDto> Rows, int TotalFiltered)> GetAdminPagedAsync(AdminListRequest request, string token, CancellationToken ct = default);
    Task<StatusCountsModel?> GetAdminCountsAsync(AdminListRequest request, string token, CancellationToken ct = default);
    Task<CommentModerationCounts?> GetModerationCountsAsync(string token, CancellationToken ct = default);

    Task<ApiCallResult> SetStatusAsync(int id, string status, string token, CancellationToken ct = default);
    Task<ApiCallResult> ReplyAsync(int id, CommentReplyFormDto dto, string token, CancellationToken ct = default);
    Task<ApiCallResult> DeleteAsync(int id, string token, CancellationToken ct = default);
    Task<ApiCallResult> ToggleActiveAsync(int id, string token, CancellationToken ct = default);
    Task<ApiCallResult> RestoreAsync(int id, string token, CancellationToken ct = default);

    Task<BulkResultModel?> BulkAsync(string action, IReadOnlyList<int> ids, string token, CancellationToken ct = default);
}
