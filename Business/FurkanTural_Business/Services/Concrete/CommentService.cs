using System.Linq.Expressions;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using FurkanTural_Application.DTOs.Comment;
using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using FurkanTural_Business.Helpers;
using FurkanTural_Business.Mappers;
using FurkanTural_Domain.Constants;
using FurkanTural_Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FurkanTural_Business.Services.Concrete;

/// <summary>Yorumların yazılması ve denetlenmesi. Ziyaretçi tarafı ile yönetici tarafı aynı serviste durur çünkü aralarındaki tek kural ortaktır: okura yalnızca onaylanmış satır çizilir.<para>Adres asla dışarı verilmez. <see cref="GetThreadAsync"/> adres taşımayan bir DTO döndürür; adresi gören tek okuma yönetim ucudur.</para><para>Bildirim postası buradan gönderilmez. Onay yalnızca kuyruğa satır açar ve işareti kaldırır; gönderim <see cref="CommentNotifier"/> turundadır. Onayın SMTP turunu beklemesi, posta sunucusu arızalıyken yöneticiyi yorum onaylayamaz hâle getirirdi — hatayı yutmak ise bildirimi sessizce düşürürdü.</para></summary>
public class CommentService(
    IUnitOfWork unitOfWork,
    ITurnstileVerifier turnstileVerifier,
    IConfiguration configuration,
    ActivityLogger activityLogger,
    CommentNotifySignal notifySignal,
    ILogger<CommentService> logger,
    IClock clock) : ICommentService
{
    /// <summary>Bir sayfada gösterilen kök yorum sayısı. Yanıtlar bu sayıya girmez; onlar köklerinin altında gelir.</summary>
    public const int ThreadPageSize = 20;

    public const int NameMaxLength = 80;
    public const int BodyMaxLength = 4000;
    public const int BodyMinLength = 2;

    /// <summary>Yanıt sayacı için tek okumada taranan en fazla satır. Sayı yalnızca silme kararına bilgi verir; sınır, bir sayfalık listenin yanıtlarının sınırsız büyümesini engeller.</summary>
    private const int ReplyScan = 1000;

    private const int ThreadNodeScan = 500;

    /// <summary>Aynı adresin aynı yazıya arka arkaya yorum bırakamayacağı süre. Asıl işi çift gönderimi yutmaktır: form iki kez gönderildiğinde ikinci satır açılmaz ve kullanıcı yine aynı olumlu metni görür.</summary>
    private static readonly TimeSpan SubmitCooldown = TimeSpan.FromSeconds(60);

    /// <summary>Yorum bırakan herkese dönen tek metin. Kaydın gerçekten açılıp açılmadığını ayırt eden bir yanıt, formu yazının hangi yorumlarının beklediğini sınayan bir araca çevirirdi.</summary>
    private const string NeutralSubmitMessage =
        "Yorumunuz alındı. Onaylandıktan sonra sayfada görünecek.";

    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ITurnstileVerifier _turnstileVerifier = turnstileVerifier;
    private readonly IConfiguration _configuration = configuration;
    private readonly ActivityLogger _activityLogger = activityLogger;
    private readonly CommentNotifySignal _notifySignal = notifySignal;
    private readonly ILogger<CommentService> _logger = logger;
    private readonly IClock _clock = clock;

    public async Task<Result<CommentThreadDto>> GetThreadAsync(int blogId, CancellationToken cancellationToken = default)
    {
        if (blogId <= 0)
            return Result<CommentThreadDto>.Fail("Yazı bulunamadı.", statusCode: 404);

        Expression<Func<Comment, bool>> approved =
            c => c.BlogId == blogId && c.Status == CommentStatuses.Approved;

        var roots = (await _unitOfWork.Comments.GetAllPagedAsync(
            1, ThreadPageSize, c => c.BlogId == blogId && c.ParentId == null && c.Status == CommentStatuses.Approved,
            false, cancellationToken)).ToList();

        var rootCount = await _unitOfWork.Comments.CountAsync(
            c => c.BlogId == blogId && c.ParentId == null && c.Status == CommentStatuses.Approved, cancellationToken);
        var totalCount = await _unitOfWork.Comments.CountAsync(approved, cancellationToken);

        var items = roots.Select(r => r.ToDto()).ToList();
        await AttachRepliesAsync(items, cancellationToken);

        return Result<CommentThreadDto>.Ok(new CommentThreadDto
        {
            BlogId = blogId,
            TotalCount = totalCount,
            RootCount = rootCount,
            PageNumber = 1,
            PageSize = ThreadPageSize,
            Items = items
        });
    }

    private async Task AttachRepliesAsync(List<CommentDto> roots, CancellationToken cancellationToken)
    {
        if (roots.Count == 0)
            return;

        var byId = roots.ToDictionary(r => r.Id);
        var frontier = roots.Select(r => r.Id).ToList();
        var scanned = 0;

        while (frontier.Count > 0 && scanned < ThreadNodeScan)
        {
            var parentIds = frontier;
            var level = (await _unitOfWork.Comments.GetAllAsync(
                c => c.ParentId != null && parentIds.Contains(c.ParentId.Value) && c.Status == CommentStatuses.Approved,
                cancellationToken)).OrderBy(c => c.Id).ToList();

            if (level.Count == 0)
                break;

            scanned += level.Count;
            frontier = [];

            foreach (var row in level)
            {
                if (!byId.TryGetValue(row.ParentId!.Value, out var parent))
                    continue;

                var dto = row.ToDto();
                parent.Replies.Add(dto);
                byId[dto.Id] = dto;
                frontier.Add(dto.Id);
            }
        }
    }

    public async Task<Result> SubmitAsync(SubmitCommentDto dto, string? turnstileToken, string? ipAddress, CancellationToken cancellationToken = default)
    {
        if (!await _turnstileVerifier.VerifyAsync(turnstileToken, ipAddress, cancellationToken))
            return Result.Fail("Bot doğrulaması başarısız. Lütfen tekrar deneyin.");

        var name = Clean(dto.AuthorName, NameMaxLength);
        if (name is null)
            return Result.Fail($"Adınızı girin (en fazla {NameMaxLength} karakter).");

        var email = NormalizeEmail(dto.AuthorEmail);
        if (email is null)
            return Result.Fail("Geçerli bir e-posta adresi girin.");

        var body = Clean(dto.Body, BodyMaxLength);
        if (body is null || body.Length < BodyMinLength)
            return Result.Fail($"Yorumunuz en az {BodyMinLength}, en fazla {BodyMaxLength} karakter olmalı.");

        var blog = await _unitOfWork.Blogs.GetByIdAsync(dto.BlogId, cancellationToken);
        if (blog is null)
            return Result.Fail("Yazı bulunamadı.", statusCode: 404);

        int? parentId = null;
        if (dto.ParentId is { } requested and > 0)
        {
            var parent = await _unitOfWork.Comments.GetByIdAsync(requested, cancellationToken);
            if (parent is null || parent.BlogId != blog.Id || parent.Status != CommentStatuses.Approved)
                return Result.Fail("Yanıtlanan yorum bulunamadı.", statusCode: 404);

            parentId = parent.Id;
        }

        var cutoff = _clock.UtcNow.Subtract(SubmitCooldown);
        var recent = await _unitOfWork.Comments.AnyAsync(
            c => c.BlogId == blog.Id && c.AuthorEmail == email && c.CreatedAt > cutoff, cancellationToken);

        if (recent)
        {
            _logger.LogInformation("Yorum kaydedilmedi: aynı adres bu yazıya {Seconds} saniye içinde yorum bırakmış.", SubmitCooldown.TotalSeconds);
            return Result.Ok(NeutralSubmitMessage);
        }

        await _unitOfWork.Comments.AddAsync(new Comment
        {
            BlogId = blog.Id,
            ParentId = parentId,
            AuthorName = name,
            AuthorEmail = email,
            Body = body,
            Status = CommentStatuses.Pending,
            NotifyOnReply = dto.NotifyOnReply
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Yorum bırakıldı, onay bekliyor. Yazı: {blog.Id}", cancellationToken);

        return Result.Ok(NeutralSubmitMessage);
    }

    public async Task<Result> DisableNotificationsAsync(string? token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return Result.Fail("Bağlantı geçersiz.");

        var hash = Hash(token.Trim());
        var notification = await _unitOfWork.CommentNotifications.GetAllForAdminAsync(n => n.TokenHash == hash, cancellationToken);
        var match = notification.FirstOrDefault();
        if (match is null || string.IsNullOrWhiteSpace(match.Email))
            return Result.Fail("Bağlantı geçersiz.");

        var rows = (await _unitOfWork.Comments.GetAllForAdminAsync(
            c => c.AuthorEmail == match.Email && c.NotifyOnReply, cancellationToken)).ToList();

        foreach (var row in rows)
        {
            row.NotifyOnReply = false;
            await _unitOfWork.Comments.UpdateAsync(row, cancellationToken);
        }

        if (rows.Count > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _activityLogger.LogAsync($"Yorum bildirimleri kapatıldı. Etkilenen yorum: {rows.Count}", cancellationToken);
        }

        return Result.Ok("Yanıt bildirimleri kapatıldı. Bu adrese bir daha yorum bildirimi göndermeyeceğiz.");
    }

    public async Task<Result<AdminCommentDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Comments.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminCommentDto>.Fail("Yorum bulunamadı.", statusCode: 404);

        var enriched = await EnrichAsync([entity], cancellationToken);
        return Result<AdminCommentDto>.Ok(enriched[0]);
    }

    public async Task<PagedResult<AdminCommentDto>> GetAllForAdminPagedAsync(AdminListQuery query, string? status, int? blogId, CancellationToken cancellationToken = default)
    {
        var predicate = AdminPredicate(query, status, blogId);
        var entities = (await _unitOfWork.Comments.GetAllForAdminPagedAsync(
            query.SafePageNumber, query.SafePageSize, predicate, true, cancellationToken)).ToList();
        var total = await _unitOfWork.Comments.CountForAdminAsync(predicate, cancellationToken);

        return PagedResult<AdminCommentDto>.Ok(
            await EnrichAsync(entities, cancellationToken), total, query.SafePageNumber, query.SafePageSize);
    }

    public async Task<Result<AdminStatusCountsDto>> GetAdminStatusCountsAsync(AdminListQuery query, string? status, int? blogId, CancellationToken cancellationToken = default)
        => Result<AdminStatusCountsDto>.Ok(
            await _unitOfWork.Comments.GetAdminStatusCountsAsync(AdminPredicate(query, status, blogId), cancellationToken));

    public async Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default)
        => Result<EntitySummaryDto>.Ok(await _unitOfWork.Comments.GetAdminSummaryAsync(cancellationToken));

    public async Task<Result<CommentModerationCountsDto>> GetModerationCountsAsync(CancellationToken cancellationToken = default)
        => Result<CommentModerationCountsDto>.Ok(new CommentModerationCountsDto
        {
            Pending = await _unitOfWork.Comments.CountForAdminAsync(c => !c.IsDeleted && c.Status == CommentStatuses.Pending, cancellationToken),
            Approved = await _unitOfWork.Comments.CountForAdminAsync(c => !c.IsDeleted && c.Status == CommentStatuses.Approved, cancellationToken),
            Rejected = await _unitOfWork.Comments.CountForAdminAsync(c => !c.IsDeleted && c.Status == CommentStatuses.Rejected, cancellationToken)
        });

    public async Task<Result<AdminCommentDto>> SetStatusAsync(int id, string? status, int? userId, CancellationToken cancellationToken = default)
    {
        var wanted = status?.Trim();
        if (!CommentStatuses.IsKnown(wanted))
            return Result<AdminCommentDto>.Fail("Geçersiz yorum durumu.");

        var entity = await _unitOfWork.Comments.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminCommentDto>.Fail("Yorum bulunamadı.", statusCode: 404);

        if (entity.IsDeleted)
            return Result<AdminCommentDto>.Fail("Silinmiş yorumun durumu değiştirilemez.", statusCode: 400);

        if (entity.Status == wanted)
            return Result<AdminCommentDto>.Ok((await EnrichAsync([entity], cancellationToken))[0]);

        entity.Status = wanted;
        entity.UpdatedBy = userId;
        if (wanted == CommentStatuses.Approved)
            entity.ApprovedAt ??= _clock.UtcNow;

        await _unitOfWork.Comments.UpdateAsync(entity, cancellationToken);

        var queued = wanted == CommentStatuses.Approved && await EnqueueNotificationAsync(entity, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Yorum durumu değiştirildi. Id: {id}, Yeni durum: {wanted}", cancellationToken);

        if (queued)
            _notifySignal.Raise();

        return Result<AdminCommentDto>.Ok((await EnrichAsync([entity], cancellationToken))[0]);
    }

    public async Task<Result<AdminCommentDto>> ReplyAsync(AdminReplyCommentDto dto, int? userId, CancellationToken cancellationToken = default)
    {
        var body = Clean(dto.Body, BodyMaxLength);
        if (body is null || body.Length < BodyMinLength)
            return Result<AdminCommentDto>.Fail($"Yanıt en az {BodyMinLength}, en fazla {BodyMaxLength} karakter olmalı.");

        var authorEmail = NormalizeEmail(_configuration["Contact:ContactEmail"]);
        if (authorEmail is null)
            return Result<AdminCommentDto>.Fail("Yanıt şu anda gönderilemiyor.", "Contact:ContactEmail yapılandırılmamış ya da geçersiz.", 500);

        var parent = await _unitOfWork.Comments.GetByIdForAdminAsync(dto.ParentId, cancellationToken);
        if (parent is null || parent.IsDeleted)
            return Result<AdminCommentDto>.Fail("Yanıtlanan yorum bulunamadı.", statusCode: 404);

        if (parent.Status != CommentStatuses.Approved)
            return Result<AdminCommentDto>.Fail("Yalnızca onaylanmış yoruma yanıt verilebilir.");

        var reply = new Comment
        {
            BlogId = parent.BlogId,
            ParentId = parent.Id,
            AuthorName = Clean(_configuration["Comments:AuthorName"], NameMaxLength) ?? "Yazar",
            AuthorEmail = authorEmail,
            Body = body,
            Status = CommentStatuses.Approved,
            ApprovedAt = _clock.UtcNow,
            NotifyOnReply = false,
            CreatedBy = userId
        };

        await _unitOfWork.Comments.AddAsync(reply, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (await EnqueueNotificationAsync(reply, cancellationToken))
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _notifySignal.Raise();
        }

        await _activityLogger.LogAsync($"Yoruma panelden yanıt verildi. Yorum: {parent.Id}, Yanıt: {reply.Id}", cancellationToken);

        return Result<AdminCommentDto>.Ok((await EnrichAsync([reply], cancellationToken))[0]);
    }

    public async Task<Result> DeleteAsync(int id, int? deletedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Comments.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null || entity.IsDeleted)
            return Result.Fail("Yorum bulunamadı.", statusCode: 404);

        await _unitOfWork.Comments.SoftDeleteAsync(entity, deletedBy, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Yorum silindi. Id: {id}", cancellationToken);

        return Result.Ok();
    }

    public async Task<Result<AdminCommentDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Comments.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminCommentDto>.Fail("Yorum bulunamadı.", statusCode: 404);

        if (entity.IsDeleted)
            return Result<AdminCommentDto>.Fail("Silinmiş kayıtların aktifliği değiştirilemez.", statusCode: 400);

        entity.IsActive = !entity.IsActive;
        entity.UpdatedBy = updatedBy;

        await _unitOfWork.Comments.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Yorum aktiflik durumu değiştirildi. Id: {id}, Yeni durum: {entity.IsActive}", cancellationToken);

        return Result<AdminCommentDto>.Ok((await EnrichAsync([entity], cancellationToken))[0]);
    }

    public async Task<Result<AdminCommentDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Comments.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminCommentDto>.Fail("Yorum bulunamadı.", statusCode: 404);

        if (!entity.IsDeleted)
            return Result<AdminCommentDto>.Fail("Bu kayıt silinmemiş, geri yükleme yapılamaz.", statusCode: 400);

        entity.UpdatedBy = updatedBy;
        await _unitOfWork.Comments.RestoreAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Yorum geri yüklendi. Id: {id}", cancellationToken);

        return Result<AdminCommentDto>.Ok((await EnrichAsync([entity], cancellationToken))[0]);
    }

    public Task<Result<BulkActionResultDto>> BulkAsync(BulkAction action, IReadOnlyCollection<int> ids, int? userId, CancellationToken cancellationToken = default)
        => BulkActions.ApplyAsync(_unitOfWork, _unitOfWork.Comments, action, ids, userId, "yorum", _activityLogger, cancellationToken);

    /// <summary>Onaylanan bir yanıt için bildirim satırı açar; açtıysa true döner. Kaydetmeyi çağırana bırakır, böylece durum değişikliği ile kuyruk satırı tek bir kaydetmede birlikte yazılır — ikisi ayrı kaydedilseydi ikincisi başarısız olduğunda yanıt yayında ama duyurusuz kalırdı.<para>Beş koşul birden aranır: satır bir yanıt olmalı, üst yorum yayında olmalı, sahibi bildirim istemiş olmalı, adres yanıtı yazanın kendi adresi olmamalı ve o yanıt için daha önce satır açılmamış olmalı. Sonuncusu tekil indeksin de koruduğu kuraldır; burada denetlenmesi kullanıcıya çakışma yerine sessiz atlama göstermek içindir.</para></summary>
    private async Task<bool> EnqueueNotificationAsync(Comment reply, CancellationToken cancellationToken)
    {
        if (reply.ParentId is not { } parentId)
            return false;

        var parent = await _unitOfWork.Comments.GetByIdForAdminAsync(parentId, cancellationToken);
        if (parent is null || parent.IsDeleted || !parent.IsActive || !parent.NotifyOnReply)
            return false;

        if (string.IsNullOrWhiteSpace(parent.AuthorEmail))
            return false;

        if (string.Equals(parent.AuthorEmail, reply.AuthorEmail, StringComparison.OrdinalIgnoreCase))
            return false;

        var existing = await _unitOfWork.CommentNotifications.GetAllForAdminAsync(n => n.CommentId == reply.Id, cancellationToken);
        if (existing.Any())
            return false;

        await _unitOfWork.CommentNotifications.AddAsync(new CommentNotification
        {
            CommentId = reply.Id,
            Email = parent.AuthorEmail,
            Status = CommentNotificationStatuses.Pending
        }, cancellationToken);

        return true;
    }

    /// <summary>Yorum satırlarına yazı başlığını, üst yorumun sahibini ve yanıt sayısını iliştirir. Üçü de toplu okunur: satır başına ayrı sorgu, listenin kendisi kadar gidiş-dönüş açardı.</summary>
    private async Task<List<AdminCommentDto>> EnrichAsync(IReadOnlyList<Comment> entities, CancellationToken cancellationToken)
    {
        if (entities.Count == 0)
            return [];

        var blogIds = entities.Select(c => c.BlogId).Distinct().ToList();
        var blogs = (await _unitOfWork.Blogs.GetAllForAdminAsync(b => blogIds.Contains(b.Id), cancellationToken))
            .ToDictionary(b => b.Id);

        var parentIds = entities.Where(c => c.ParentId is not null).Select(c => c.ParentId!.Value).Distinct().ToList();
        var parents = parentIds.Count == 0
            ? []
            : (await _unitOfWork.Comments.GetAllForAdminAsync(c => parentIds.Contains(c.Id), cancellationToken))
                .ToDictionary(c => c.Id, c => c.AuthorName);

        var ids = entities.Select(c => c.Id).ToList();
        var replyRows = await _unitOfWork.Comments.SelectForAdminPagedAsync(
            1, ReplyScan,
            c => new Comment { Id = c.Id, ParentId = c.ParentId },
            c => c.ParentId != null && ids.Contains(c.ParentId.Value),
            cancellationToken: cancellationToken);

        var replyCounts = replyRows
            .GroupBy(r => r.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        return [.. entities.Select(c =>
        {
            blogs.TryGetValue(c.BlogId, out var blog);
            return c.ToAdminDto(
                blog?.Title,
                blog?.Slug,
                c.ParentId is { } pid && parents.TryGetValue(pid, out var parentName) ? parentName : null,
                replyCounts.GetValueOrDefault(c.Id));
        })];
    }

    private static Expression<Func<Comment, bool>>? AdminPredicate(AdminListQuery query, string? status, int? blogId)
    {
        var predicate = AdminFilters.Common<Comment>(query);

        if (CommentStatuses.IsKnown(status?.Trim()))
        {
            var wanted = status!.Trim();
            predicate = predicate.AndAlso(x => x.Status == wanted);
        }

        if (blogId is { } id and > 0)
            predicate = predicate.AndAlso(x => x.BlogId == id);

        if (query.SearchTerm is { } term)
            predicate = predicate.AndAlso(x =>
                (x.AuthorName != null && x.AuthorName.Contains(term)) ||
                (x.AuthorEmail != null && x.AuthorEmail.Contains(term)) ||
                (x.Body != null && x.Body.Contains(term)));

        return predicate;
    }

    private static string? Clean(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        return trimmed.Length > max ? null : trimmed;
    }

    private static string? NormalizeEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        var trimmed = email.Trim();
        if (trimmed.Length > 256 || !MailAddress.TryCreate(trimmed, out var parsed) || parsed.Address != trimmed)
            return null;

        return trimmed.ToLowerInvariant();
    }

    private static string Hash(string token)
        => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
