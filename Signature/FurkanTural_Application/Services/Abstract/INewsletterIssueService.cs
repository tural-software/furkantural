using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.DTOs.Newsletter;
using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

/// <summary>Bülten sayılarının yazımı ve dağıtıma verilmesi. <see cref="INewsletterService"/> listeye kimin girdiğiyle ilgilenir, burası o listeye ne gönderildiğiyle; ikisinin ortak kuralı tektir — yalnızca doğrulanmış adres posta alır.<para>Uçların tamamı yönetim uçlarıdır ve ziyaretçiye açık karşılığı yoktur.</para><para><see cref="QueueAsync"/> geri alınamaz sınırdır: alıcı listesi o anda dondurulur, her alıcı için bir dağıtım satırı açılır ve konu ile gövde bir daha değiştirilemez. Sınırın gerekçesi basit — dağıtım sürerken metni değiştirmek, listenin bir bölümüne eski, bir bölümüne yeni metni göndermek demektir.</para><para>Gönderimi durdurmanın ayrı bir ucu yoktur; sayıyı pasife almak ya da silmek dağıtıcının görüş alanından çıkarır ve dağıtım durur. Geri açıldığında kaldığı yerden sürer, çünkü kime gönderildiği tek tek kayıtlıdır.</para></summary>
public interface INewsletterIssueService
{
    Task<Result<AdminNewsletterIssueDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default);
    Task<PagedResult<AdminNewsletterIssueDto>> GetAllForAdminPagedAsync(AdminListQuery query, CancellationToken cancellationToken = default);
    Task<Result<AdminStatusCountsDto>> GetAdminStatusCountsAsync(AdminListQuery query, CancellationToken cancellationToken = default);
    Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default);

    Task<Result<AdminNewsletterIssueDto>> CreateAsync(CreateNewsletterIssueDto dto, int? userId, CancellationToken cancellationToken = default);

    /// <summary>Taslağı günceller. Yalnızca taslak durumundaki sayı düzenlenebilir; dağıtıma verilmiş bir sayının metni değiştirilemez.</summary>
    Task<Result<AdminNewsletterIssueDto>> UpdateAsync(UpdateNewsletterIssueDto dto, int? userId, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(int id, int? deletedBy, CancellationToken cancellationToken = default);
    Task<Result<AdminNewsletterIssueDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<AdminNewsletterIssueDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<BulkActionResultDto>> BulkAsync(BulkAction action, IReadOnlyCollection<int> ids, int? userId, CancellationToken cancellationToken = default);

    /// <summary>Alıcı listesini dondurur ve sayıyı dağıtıma verir. Listeye yalnızca doğrulanmış, aktif ve silinmemiş adresler girer; dondurmadan sonra abone olan kimse bu sayıyı almaz.<para>Postayı bu metot göndermez, yalnızca kuyruğu kurar. Gönderim <see cref="INewsletterDispatcher"/> tarafından arka planda yapılır; böylece yöneticinin isteği bin adresin SMTP turunu beklemez ve tarayıcının kapatılması dağıtımı kesmez.</para></summary>
    Task<Result<NewsletterIssueProgressDto>> QueueAsync(int id, int? userId, CancellationToken cancellationToken = default);

    Task<Result<NewsletterIssueProgressDto>> GetProgressAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Şu anda dağıtıma girecek adres sayısı. <see cref="QueueAsync"/> ile aynı süzgeci kullanır, dolayısıyla gönderim öncesi gösterilen sayı ile dondurulan liste ayrışamaz.</summary>
    Task<Result<int>> GetAudienceCountAsync(CancellationToken cancellationToken = default);

    /// <summary>Sayıyı tek bir adrese deneme olarak gönderir. Dağıtım satırı açmaz, durumu değiştirmez, sayaçlara dokunmaz; taslak da dağıtılmış da olsa çalışır.<para>Çıkış bağlantısı jetonsuz gider ve çıkış formuna düşer: deneme alıcısı abone olmayabilir, ona gerçek bir çıkış jetonu üretmek listeye ait olmayan bir kimlik bilgisi yaratmak olurdu.</para></summary>
    Task<Result> SendTestAsync(int id, string? email, CancellationToken cancellationToken = default);
}
