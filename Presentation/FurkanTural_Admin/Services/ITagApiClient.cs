using FurkanTural_Admin.Helpers;
using FurkanTural_Admin.Models.Common;
using FurkanTural_Admin.Models.Tag;

namespace FurkanTural_Admin.Services;

/// <summary>Yazma uçları <see cref="ApiCallResult"/> döndürür: etiket adı tekil olduğu için "bu adda bir etiket zaten var" en sık karşılaşılacak reddir ve onu tek bir "işlem başarısız" metnine indirmek, kullanıcıyı sebebi görünmeyen bir hatayla baş başa bırakırdı.</summary>
public interface ITagApiClient
{
    Task<IReadOnlyList<TagAdminDto>> GetAllForAdminAsync(string token, CancellationToken ct = default);
    Task<(IReadOnlyList<TagAdminDto> Rows, int TotalFiltered)> GetAdminPagedAsync(AdminListRequest request, string token, CancellationToken ct = default);
    Task<StatusCountsModel?> GetAdminCountsAsync(AdminListRequest request, string token, CancellationToken ct = default);

    Task<ApiCallResult> CreateAsync(TagFormDto dto, string token, CancellationToken ct = default);
    Task<ApiCallResult> UpdateAsync(int id, TagFormDto dto, string token, CancellationToken ct = default);
    Task<ApiCallResult> DeleteAsync(int id, string token, CancellationToken ct = default);
    Task<ApiCallResult> ToggleActiveAsync(int id, string token, CancellationToken ct = default);
    Task<ApiCallResult> RestoreAsync(int id, string token, CancellationToken ct = default);

    Task<BulkResultModel?> BulkAsync(string action, IReadOnlyList<int> ids, string token, CancellationToken ct = default);
}
