using FurkanTural_Admin.Helpers;
using FurkanTural_Admin.Models.Common;
using FurkanTural_Admin.Models.NewsletterIssue;

namespace FurkanTural_Admin.Services;

/// <summary>Yazma uçları <see cref="ApiCallResult"/> döndürür, <c>bool</c> değil: bültenin reddedilme sebepleri yöneticinin doğrudan işine yarar ("doğrulanmış abone yok", "şablon pasif", "dağıtıma verilmiş bülten düzenlenemez") ve hepsini tek bir "işlem başarısız" metnine indirmek panelin en çok bilgi gereken ekranını en sessiz ekranı yapardı.</summary>
public interface INewsletterIssueApiClient
{
    Task<(IReadOnlyList<NewsletterIssueAdminDto> Rows, int TotalFiltered)> GetAdminPagedAsync(AdminListRequest request, string token, CancellationToken ct = default);
    Task<StatusCountsModel?> GetAdminCountsAsync(AdminListRequest request, string token, CancellationToken ct = default);
    Task<int> GetAudienceCountAsync(string token, CancellationToken ct = default);
    Task<NewsletterIssueProgressModel?> GetProgressAsync(int id, string token, CancellationToken ct = default);
    Task<string?> GetBodyAsync(int id, string token, CancellationToken ct = default);

    Task<ApiCallResult> CreateAsync(NewsletterIssueFormDto dto, string token, CancellationToken ct = default);
    Task<ApiCallResult> UpdateAsync(int id, NewsletterIssueFormDto dto, string token, CancellationToken ct = default);
    Task<ApiCallResult> DeleteAsync(int id, string token, CancellationToken ct = default);
    Task<ApiCallResult> ToggleActiveAsync(int id, string token, CancellationToken ct = default);
    Task<ApiCallResult> RestoreAsync(int id, string token, CancellationToken ct = default);
    Task<ApiCallResult> QueueAsync(int id, string token, CancellationToken ct = default);
    Task<ApiCallResult> SendTestAsync(int id, string? email, string token, CancellationToken ct = default);

    Task<BulkResultModel?> BulkAsync(string action, IReadOnlyList<int> ids, string token, CancellationToken ct = default);
}
