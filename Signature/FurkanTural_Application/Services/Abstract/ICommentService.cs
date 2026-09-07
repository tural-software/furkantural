using FurkanTural_Application.DTOs.Comment;
using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

/// <summary>Yazı altındaki yorumlar. İki tarafı vardır ve ikisi bilerek asimetriktir: ziyaretçi yalnızca yazar ve okur, yayına girme kararı yöneticiye aittir.<para><b>Ön denetim.</b> Yeni yorum daima beklemede açılır ve okura görünmez. Sonradan denetlemek — önce yayımlayıp sonra temizlemek — spam'i okurun karşısına koyar ve temizliğin yetişme hızına bağlar; bu sitede yayımlanan her satırın onaylanmış olması tercih edilir. Bedeli, yazanın yorumunu anında görememesidir ve bu bedel ekranda açıkça söylenerek ödenir.</para><para><b>Kimlik yoktur.</b> Ad ile adres beyandır, doğrulanmaz. Adres yayımlanmaz; iki işi vardır — aynı kişiyi turdan tura tanımak ve istenmişse yanıt bildirimi göndermek. <see cref="GetThreadAsync"/> adresi hiç döndürmez.</para><para><b>Tek düzey.</b> Bir yanıtın yanıtı olamaz. Sınır yapısal değil sunumsaldır: dar ekranda üçüncü seviyeden sonra metin sütunu okunmaz hâle gelir.</para><para><b>Bildirim postası buradan gönderilmez.</b> <see cref="SetStatusAsync"/> yalnızca kuyruğa satır açar; postayı <see cref="ICommentNotifier"/> arka planda gönderir. Onayın SMTP turunu beklemesi, posta sunucusu arızalıyken yöneticiyi yorum onaylayamaz hâle getirirdi.</para></summary>
public interface ICommentService : IBulkService
{
    /// <summary>Bir yazının yayındaki yorumları, yanıtları kök yorumların altına yerleştirilmiş olarak. Adres taşımaz.</summary>
    Task<Result<CommentThreadDto>> GetThreadAsync(int blogId, CancellationToken cancellationToken = default);

    /// <summary>Ziyaretçinin yorumunu beklemede kaydeder. Bot doğrulaması koşulsuzdur: iletişim formu ve bülten kaydıyla aynı gerekçeyle, uygulamaya göre koşullu çalışan bir model bu akışta doğrulamayı sessizce hiç çalıştırmazdı.<para>Yanıt daima aynıdır — yorumun alındığı ve onaydan sonra görüneceği. Kaydın gerçekten açılıp açılmadığını ayırt eden bir metin, formu yazının hangi yorumlarının beklediğini sınayan bir araca çevirirdi.</para></summary>
    Task<Result> SubmitAsync(SubmitCommentDto dto, string? turnstileToken, string? ipAddress, CancellationToken cancellationToken = default);

    /// <summary>Bildirim postasındaki çıkış jetonunu harcar ve o adresin bütün yorumlarında yanıt bildirimini kapatır. Tek yorum için değil adres için kapatılır: "bana posta göndermeyi bırak" diyen biri yalnızca o tek yorum için söylemiyordur.</summary>
    Task<Result> DisableNotificationsAsync(string? token, CancellationToken cancellationToken = default);

    Task<Result<AdminCommentDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default);
    Task<PagedResult<AdminCommentDto>> GetAllForAdminPagedAsync(AdminListQuery query, string? status, int? blogId, CancellationToken cancellationToken = default);
    Task<Result<AdminStatusCountsDto>> GetAdminStatusCountsAsync(AdminListQuery query, string? status, int? blogId, CancellationToken cancellationToken = default);
    Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default);

    /// <summary>Denetim kuyruğunun durum sayaçları; süzgeçlerden bağımsızdır ve bekleyen yorum olup olmadığını tek okumada söyler.</summary>
    Task<Result<CommentModerationCountsDto>> GetModerationCountsAsync(CancellationToken cancellationToken = default);

    /// <summary>Yorumun denetim durumunu değiştirir. Onaya geçen bir yanıt, üst yorumun sahibi bildirim istediyse kuyruğa bir satır açar; aynı yanıt için ikinci satır açılmaz, dolayısıyla reddedilip yeniden onaylanan bir yanıt ikinci posta üretmez.</summary>
    Task<Result<AdminCommentDto>> SetStatusAsync(int id, string? status, int? userId, CancellationToken cancellationToken = default);

    /// <summary>Yazının sahibi olarak yanıt verir. Yanıt beklemeden yayına girer ve ziyaretçi formundaki bot doğrulaması aranmaz; kimlik yapılandırmadan okunur.<para><see cref="SetStatusAsync"/>'ten bir yerde ayrılır: orada durum değişikliği ile bildirim satırı tek kaydetmede birlikte yazılır, burada iki kaydetme gerekir çünkü bildirim satırı yanıtın kimliğine bağlıdır ve o kimlik ilk kaydetmeden önce yoktur. Kalan tek arıza biçimi "yanıt yayında ama duyurulmadı"dır; tersi — duyurulmuş ama yayında olmayan bir yanıt — mümkün değildir.</para></summary>
    Task<Result<AdminCommentDto>> ReplyAsync(AdminReplyCommentDto dto, int? userId, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(int id, int? deletedBy, CancellationToken cancellationToken = default);
    Task<Result<AdminCommentDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<AdminCommentDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
}
