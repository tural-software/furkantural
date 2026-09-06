using System.Linq.Expressions;
using System.Net.Mail;
using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.DTOs.Mail;
using FurkanTural_Application.DTOs.Newsletter;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using FurkanTural_Business.Helpers;
using FurkanTural_Business.Mappers;
using FurkanTural_Domain.Constants;
using FurkanTural_Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace FurkanTural_Business.Services.Concrete;

/// <summary>Bülten sayısının yazımı ve dağıtıma verilmesi. Gönderimin kendisi burada değil <see cref="NewsletterDispatcher"/> içindedir; bu ayrım, yöneticinin isteğinin yüzlerce SMTP turunu beklememesi ve tarayıcının kapatılmasının dağıtımı kesmemesi içindir.<para>Alıcı listesi <see cref="QueueAsync"/> anında dondurulur ve her alıcı için bir dağıtım satırı açılır. Satırların tamamı ile durum değişimi <b>tek</b> kaydetmede yazılır: iki ayrı kaydetme, arada düşen bir süreçte satırları açılmış ama taslak görünen bir sayı bırakırdı ve ikinci deneme tekil indekse takılıp kalıcı olarak tıkanırdı.</para><para>Dağıtım öncesi şablon denetimi bilerek buradadır. Şablon eksikse gönderim alıcı alıcı başarısız olurdu; kuyruğu hiç kurmamak, beş yüz satırı tek tek yakmaktan iyidir.</para></summary>
public class NewsletterIssueService(
    IUnitOfWork unitOfWork,
    IMailSender mailSender,
    IConfiguration configuration,
    NewsletterDispatchSignal dispatchSignal,
    ActivityLogger activityLogger,
    IClock clock) : INewsletterIssueService
{
    /// <summary>Alıcılar bu büyüklükte sayfalarla okunur. Sorgu sınırlı kalır ama satırlar bellekte toplanır: dondurma tek kaydetmede yazılmak zorunda olduğu için işlemin boyutu zaten listenin kendi büyüklüğüdür.</summary>
    public const int RecipientPageSize = 500;

    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IMailSender _mailSender = mailSender;
    private readonly IConfiguration _configuration = configuration;
    private readonly NewsletterDispatchSignal _dispatchSignal = dispatchSignal;
    private readonly ActivityLogger _activityLogger = activityLogger;
    private readonly IClock _clock = clock;

    public async Task<Result<AdminNewsletterIssueDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.NewsletterIssues.GetByIdForAdminAsync(id, cancellationToken);
        return entity is null
            ? Result<AdminNewsletterIssueDto>.Fail("Bülten bulunamadı.", statusCode: 404)
            : Result<AdminNewsletterIssueDto>.Ok(entity.ToAdminDto());
    }

    public async Task<PagedResult<AdminNewsletterIssueDto>> GetAllForAdminPagedAsync(AdminListQuery query, CancellationToken cancellationToken = default)
    {
        var predicate = AdminPredicate(query);
        var entities = await _unitOfWork.NewsletterIssues.GetAllForAdminPagedAsync(query.SafePageNumber, query.SafePageSize, predicate, true, cancellationToken);
        var total = await _unitOfWork.NewsletterIssues.CountForAdminAsync(predicate, cancellationToken);
        return PagedResult<AdminNewsletterIssueDto>.Ok(entities.Select(e => e.ToAdminDto()), total, query.SafePageNumber, query.SafePageSize);
    }

    public async Task<Result<AdminStatusCountsDto>> GetAdminStatusCountsAsync(AdminListQuery query, CancellationToken cancellationToken = default)
        => Result<AdminStatusCountsDto>.Ok(await _unitOfWork.NewsletterIssues.GetAdminStatusCountsAsync(AdminPredicate(query), cancellationToken));

    public async Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default)
        => Result<EntitySummaryDto>.Ok(await _unitOfWork.NewsletterIssues.GetAdminSummaryAsync(cancellationToken));

    public async Task<Result<AdminNewsletterIssueDto>> CreateAsync(CreateNewsletterIssueDto dto, int? userId, CancellationToken cancellationToken = default)
    {
        var invalid = Validate(dto.Subject, dto.Body);
        if (invalid is not null)
            return Result<AdminNewsletterIssueDto>.Fail(invalid);

        var entity = new NewsletterIssue
        {
            Subject = dto.Subject!.Trim(),
            Body = dto.Body,
            Status = NewsletterIssueStatuses.Draft,
            CreatedBy = userId
        };

        await _unitOfWork.NewsletterIssues.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Bülten taslağı oluşturuldu. Id: {entity.Id}", cancellationToken);

        return Result<AdminNewsletterIssueDto>.Ok(entity.ToAdminDto());
    }

    public async Task<Result<AdminNewsletterIssueDto>> UpdateAsync(UpdateNewsletterIssueDto dto, int? userId, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.NewsletterIssues.GetByIdForAdminAsync(dto.Id, cancellationToken);
        if (entity is null)
            return Result<AdminNewsletterIssueDto>.Fail("Bülten bulunamadı.", statusCode: 404);

        if (entity.Status != NewsletterIssueStatuses.Draft)
            return Result<AdminNewsletterIssueDto>.Fail("Dağıtıma verilmiş bülten düzenlenemez.", statusCode: 409);

        var invalid = Validate(dto.Subject, dto.Body);
        if (invalid is not null)
            return Result<AdminNewsletterIssueDto>.Fail(invalid);

        entity.Subject = dto.Subject!.Trim();
        entity.Body = dto.Body;
        entity.UpdatedBy = userId;

        await _unitOfWork.NewsletterIssues.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Bülten taslağı güncellendi. Id: {entity.Id}", cancellationToken);

        return Result<AdminNewsletterIssueDto>.Ok(entity.ToAdminDto());
    }

    public async Task<Result> DeleteAsync(int id, int? deletedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.NewsletterIssues.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result.Fail("Bülten bulunamadı.", statusCode: 404);

        await _unitOfWork.NewsletterIssues.SoftDeleteAsync(entity, deletedBy, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Bülten silindi. Id: {id}", cancellationToken);

        return Result.Ok();
    }

    /// <summary>Aktiflik anahtarı dağıtıma verilmiş bir sayıda aynı zamanda duraklat/sürdür düğmesidir: pasif kayıt küresel süzgeç yüzünden dağıtıcının görüş alanından çıkar ve gönderim durur. Kime gönderildiği tek tek kayıtlı olduğu için geri açıldığında kaldığı yerden sürer.</summary>
    public async Task<Result<AdminNewsletterIssueDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.NewsletterIssues.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminNewsletterIssueDto>.Fail("Bülten bulunamadı.", statusCode: 404);

        if (entity.IsDeleted)
            return Result<AdminNewsletterIssueDto>.Fail("Silinmiş kayıtların aktifliği değiştirilemez.", statusCode: 400);

        entity.IsActive = !entity.IsActive;
        entity.UpdatedBy = updatedBy;
        await _unitOfWork.NewsletterIssues.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (entity.Status == NewsletterIssueStatuses.Sending)
            await _activityLogger.LogAsync(
                entity.IsActive
                    ? $"Bülten dağıtımı sürdürüldü. Id: {entity.Id}"
                    : $"Bülten dağıtımı duraklatıldı. Id: {entity.Id}",
                cancellationToken);

        if (entity.IsActive)
            _dispatchSignal.Raise();

        return Result<AdminNewsletterIssueDto>.Ok(entity.ToAdminDto());
    }

    public async Task<Result<AdminNewsletterIssueDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.NewsletterIssues.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminNewsletterIssueDto>.Fail("Bülten bulunamadı.", statusCode: 404);

        if (!entity.IsDeleted)
            return Result<AdminNewsletterIssueDto>.Fail("Bu kayıt silinmemiş, geri yükleme yapılamaz.", statusCode: 400);

        entity.UpdatedBy = updatedBy;
        await _unitOfWork.NewsletterIssues.RestoreAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _dispatchSignal.Raise();

        return Result<AdminNewsletterIssueDto>.Ok(entity.ToAdminDto());
    }

    public Task<Result<BulkActionResultDto>> BulkAsync(BulkAction action, IReadOnlyCollection<int> ids, int? userId, CancellationToken cancellationToken = default)
        => BulkActions.ApplyAsync(_unitOfWork, _unitOfWork.NewsletterIssues, action, ids, userId, "bülten", _activityLogger, cancellationToken);

    public async Task<Result<NewsletterIssueProgressDto>> QueueAsync(int id, int? userId, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.NewsletterIssues.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<NewsletterIssueProgressDto>.Fail("Bülten bulunamadı.", statusCode: 404);

        if (entity.IsDeleted)
            return Result<NewsletterIssueProgressDto>.Fail("Silinmiş bülten dağıtıma verilemez.", statusCode: 400);

        if (entity.Status != NewsletterIssueStatuses.Draft)
            return Result<NewsletterIssueProgressDto>.Fail("Bu bülten zaten dağıtıma verilmiş.", statusCode: 409);

        if (string.IsNullOrWhiteSpace(_configuration["Newsletter:UnsubscribeUrl"]))
            return Result<NewsletterIssueProgressDto>.Fail(
                "Bülten gönderilemiyor: çıkış adresi yapılandırılmamış.", "Newsletter:UnsubscribeUrl yapılandırılmamış.", 500);

        if (!await TemplateExistsAsync(cancellationToken))
            return Result<NewsletterIssueProgressDto>.Fail(
                "Bülten gönderilemiyor: bülten posta şablonu yok ya da pasif.",
                $"{MailTemplateDefinitions.NewsletterIssue} türü için etkin şablon bulunamadı.", 500);

        var recipients = await RecipientsAsync(cancellationToken);
        if (recipients.Count == 0)
            return Result<NewsletterIssueProgressDto>.Fail("Doğrulanmış abone yok; gönderilecek adres bulunamadı.", statusCode: 400);

        await _unitOfWork.NewsletterDeliveries.AddRangeAsync(recipients.Select(r => new NewsletterDelivery
        {
            NewsletterIssueId = entity.Id,
            SubscriberId = r.Id,
            Email = r.Email,
            Status = NewsletterDeliveryStatuses.Pending,
            CreatedBy = userId
        }), cancellationToken);

        entity.Status = NewsletterIssueStatuses.Sending;
        entity.QueuedAt = _clock.UtcNow;
        entity.RecipientCount = recipients.Count;
        entity.SentCount = 0;
        entity.FailedCount = 0;
        entity.SkippedCount = 0;
        entity.UpdatedBy = userId;
        await _unitOfWork.NewsletterIssues.UpdateAsync(entity, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Bülten dağıtıma verildi. Id: {entity.Id}, Alıcı: {recipients.Count}", cancellationToken);
        _dispatchSignal.Raise();

        return Result<NewsletterIssueProgressDto>.Ok(
            entity.ToProgressDto(recipients.Count), "Bülten dağıtıma verildi. Gönderim arka planda sürüyor.");
    }

    public async Task<Result<NewsletterIssueProgressDto>> GetProgressAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.NewsletterIssues.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<NewsletterIssueProgressDto>.Fail("Bülten bulunamadı.", statusCode: 404);

        var pending = entity.Status == NewsletterIssueStatuses.Draft
            ? 0
            : await _unitOfWork.NewsletterDeliveries.CountAsync(
                d => d.NewsletterIssueId == entity.Id && d.Status == NewsletterDeliveryStatuses.Pending, cancellationToken);

        return Result<NewsletterIssueProgressDto>.Ok(entity.ToProgressDto(pending));
    }

    public async Task<Result> SendTestAsync(int id, string? email, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.NewsletterIssues.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result.Fail("Bülten bulunamadı.", statusCode: 404);

        var address = Normalize(email);
        if (address is null)
            return Result.Fail("Geçerli bir e-posta adresi girin.");

        var unsubscribeUrl = _configuration["Newsletter:UnsubscribeUrl"];
        if (string.IsNullOrWhiteSpace(unsubscribeUrl))
            return Result.Fail("Deneme gönderilemiyor: çıkış adresi yapılandırılmamış.", "Newsletter:UnsubscribeUrl yapılandırılmamış.", 500);

        var sent = await _mailSender.SendAsync(
            MailTemplateDefinitions.NewsletterIssue, AppSourceDefinitions.Blog, address,
            new NewsletterIssueMailDto
            {
                Subject = entity.Subject,
                Body = entity.Body,
                Email = address,
                UnsubscribeUrl = unsubscribeUrl,
                ContactEmail = _configuration["Contact:ContactEmail"] ?? "",
                CurrentYear = _clock.UtcNow.Year.ToString()
            },
            cancellationToken);

        if (sent.IsFailure)
            return Result.Fail("Deneme gönderilemedi.", sent.InternalMessage, sent.StatusCode);

        await _activityLogger.LogAsync($"Bülten denemesi gönderildi. Id: {entity.Id}", cancellationToken);
        return Result.Ok("Deneme gönderildi.");
    }

    public async Task<Result<int>> GetAudienceCountAsync(CancellationToken cancellationToken = default)
        => Result<int>.Ok(await _unitOfWork.Subscribers.CountForAdminAsync(Audience, cancellationToken));

    /// <summary>Dağıtıma girecek adresin tek tanımı. Hem sayım hem dondurma buradan okur; iki ayrı yerde yazılsaydı yöneticiye gösterilen sayı ile gerçekte dondurulan liste sessizce ayrışabilirdi.<para>Süzgeç elle yazılır çünkü bu okumalar küresel süzgeci atlar; koşulun görünür durması, listeye kimin girdiğini tek bakışta okunur kılar.</para></summary>
    private static readonly Expression<Func<Subscriber, bool>> Audience =
        s => !s.IsDeleted && s.IsActive && s.ConfirmedAt != null && s.Email != null;

    /// <summary>Projeksiyon iki alanla sınırlıdır: dondurma için gereken tek şey kimlik ve adrestir, kalan sütunları okumak listenin tamamı için boşuna taşınan veri olurdu.</summary>
    private async Task<List<Subscriber>> RecipientsAsync(CancellationToken cancellationToken)
    {
        var recipients = new List<Subscriber>();

        for (var page = 1; ; page++)
        {
            var batch = (await _unitOfWork.Subscribers.SelectForAdminPagedAsync(
                page, RecipientPageSize,
                s => new Subscriber { Id = s.Id, Email = s.Email },
                Audience,
                false, cancellationToken)).ToList();

            recipients.AddRange(batch);

            if (batch.Count < RecipientPageSize) break;
        }

        return recipients;
    }

    private async Task<bool> TemplateExistsAsync(CancellationToken cancellationToken)
    {
        var type = await _unitOfWork.MailTemplateTypes.GetAsync(x => x.Code == MailTemplateDefinitions.NewsletterIssue, cancellationToken);
        return type is not null && await _unitOfWork.MailTemplates.AnyAsync(x => x.MailTemplateTypeId == type.Id, cancellationToken);
    }

    private static string? Validate(string? subject, string? body)
    {
        if (string.IsNullOrWhiteSpace(subject)) return "Konu boş olamaz.";
        if (subject.Trim().Length > 300) return "Konu en fazla 300 karakter olabilir.";
        if (string.IsNullOrWhiteSpace(body)) return "Bülten gövdesi boş olamaz.";
        return null;
    }

    private static string? Normalize(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;

        var trimmed = email.Trim();
        if (trimmed.Length > 256 || !MailAddress.TryCreate(trimmed, out var parsed) || parsed.Address != trimmed)
            return null;

        return trimmed.ToLowerInvariant();
    }

    private static Expression<Func<NewsletterIssue, bool>>? AdminPredicate(AdminListQuery query)
    {
        var predicate = AdminFilters.Common<NewsletterIssue>(query);
        if (query.SearchTerm is { } term)
            predicate = predicate.AndAlso(x => x.Subject != null && x.Subject.Contains(term));
        return predicate;
    }
}
