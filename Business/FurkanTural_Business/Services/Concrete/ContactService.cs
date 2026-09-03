using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.DTOs.Contact;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using FurkanTural_Business.Helpers;
using FurkanTural_Application.DTOs.Mail;
using FurkanTural_Business.Mappers;
using FurkanTural_Domain.Constants;
using Microsoft.Extensions.Configuration;

namespace FurkanTural_Business.Services.Concrete;

/// <summary>Mesaj kaydedildikten sonra iki bildirim gönderilir: biri site sahibine, biri gönderene. Konu ve gövde koda gömülü değildir, <see cref="IMailSender"/> üzerinden veri tabanındaki şablondan gelir; şablon yoksa o posta gönderilmez.<para>Gönderim hatası akışı düşürmez — form yanıtı kaydedilmiş mesaja göre verilir, posta kutusuna göre değil — ama sessizce yutulmaz da: başarısızlığın gerekçesi denetim kaydına yazılır, yoksa gönderilmeyen postanın hiçbir izi kalmazdı.</para></summary>
public class ContactService(
    IUnitOfWork unitOfWork,
    IMailSender mailSender,
    IConfiguration configuration,
    ActivityLogger activityLogger,
    ITurnstileVerifier turnstileVerifier,
    IClock clock) : IContactService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IMailSender _mailSender = mailSender;
    private readonly IConfiguration _configuration = configuration;
    private readonly ActivityLogger _activityLogger = activityLogger;
    private readonly ITurnstileVerifier _turnstileVerifier = turnstileVerifier;
    private readonly IClock _clock = clock;

    public async Task<Result> SubmitAsync(SubmitContactDto dto, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default)
    {
        if (!await _turnstileVerifier.VerifyAsync(dto.TurnstileToken, ipAddress, cancellationToken))
            return Result.Fail("Robot doğrulaması başarısız oldu. Lütfen tekrar deneyin.");

        if (string.IsNullOrWhiteSpace(dto.Name))
            return Result.Fail("Ad alanı boş olamaz.");
        if (string.IsNullOrWhiteSpace(dto.Email))
            return Result.Fail("E-posta alanı boş olamaz.");
        if (string.IsNullOrWhiteSpace(dto.Message))
            return Result.Fail("Mesaj alanı boş olamaz.");

        var createDto = new CreateContactDto
        {
            Name = dto.Name,
            Email = dto.Email,
            Message = dto.Message,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };

        var entity = createDto.ToEntity();
        await _unitOfWork.Contacts.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Yeni iletişim mesajı alındı. Id: {entity.Id}", cancellationToken);

        await SendEmailsAsync(dto, ipAddress, userAgent, cancellationToken);

        return Result.Ok();
    }

    private async Task SendEmailsAsync(SubmitContactDto dto, string? ipAddress, string? userAgent, CancellationToken ct)
    {
        var now = _clock.UtcNow;
        var createdAt = now.ToString("dd.MM.yyyy HH:mm");

        var ownerResult = await _mailSender.SendAsync(
            MailTemplateDefinitions.ContactOwner,
            AppSourceDefinitions.Portfolio,
            _configuration["Contact:OwnerEmail"] ?? "furkanturalofficial@outlook.com",
            new ContactOwnerMailDto
            {
                FullName = dto.Name,
                Email = dto.Email,
                Message = dto.Message,
                CreatedAt = createdAt,
                IpAddress = ipAddress,
                Browser = userAgent,
                FormPageUrl = _configuration["Contact:FormPageUrl"] ?? ""
            }, ct);

        if (ownerResult.IsFailure)
            await _activityLogger.LogAsync($"İletişim bildirimi gönderilemedi (site sahibi): {ownerResult.InternalMessage}", ct);

        var userResult = await _mailSender.SendAsync(
            MailTemplateDefinitions.ContactUser,
            AppSourceDefinitions.Portfolio,
            dto.Email,
            new ContactUserMailDto
            {
                FullName = dto.Name,
                Email = dto.Email,
                Message = dto.Message,
                CreatedAt = createdAt,
                CurrentYear = now.Year.ToString(),
                ContactEmail = _configuration["Contact:ContactEmail"] ?? "",
                LinkedInUrl = _configuration["Contact:LinkedInUrl"] ?? "",
                GitHubUrl = _configuration["Contact:GitHubUrl"] ?? "",
                InstagramUrl = _configuration["Contact:InstagramUrl"] ?? ""
            }, ct);

        if (userResult.IsFailure)
            await _activityLogger.LogAsync($"İletişim yanıtı gönderilemedi (gönderen): {userResult.InternalMessage}", ct);
    }

    public async Task<Result<ContactDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Contacts.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return Result<ContactDto>.Fail("İletişim mesajı bulunamadı.", statusCode: 404);
        return Result<ContactDto>.Ok(entity.ToDto());
    }

    public async Task<Result<IEnumerable<ContactDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.Contacts.GetAllAsync(cancellationToken);
        return Result<IEnumerable<ContactDto>>.Ok(entities.Select(e => e.ToDto()));
    }

    public async Task<PagedResult<ContactDto>> GetAllPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.Contacts.GetAllPagedAsync(pageNumber, pageSize, cancellationToken: cancellationToken);
        var total = await _unitOfWork.Contacts.CountAsync(cancellationToken: cancellationToken);
        return PagedResult<ContactDto>.Ok(entities.Select(e => e.ToDto()), total, pageNumber, pageSize);
    }

    public async Task<Result<IEnumerable<AdminContactDto>>> GetAllForAdminAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.Contacts.GetAllForAdminAsync(cancellationToken);
        return Result<IEnumerable<AdminContactDto>>.Ok(entities.Select(e => e.ToAdminDto()));
    }

    public async Task<Result<AdminContactDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Contacts.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminContactDto>.Fail("İletişim mesajı bulunamadı.", statusCode: 404);
        return Result<AdminContactDto>.Ok(entity.ToAdminDto());
    }

    public async Task<Result<AdminContactDto>> MarkAsReadAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Contacts.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminContactDto>.Fail("İletişim mesajı bulunamadı.", statusCode: 404);

        entity.IsRead = true;
        await _unitOfWork.Contacts.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AdminContactDto>.Ok(entity.ToAdminDto());
    }

    public async Task<Result<AdminContactDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Contacts.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminContactDto>.Fail("İletişim mesajı bulunamadı.", statusCode: 404);

        if (entity.IsDeleted)
            return Result<AdminContactDto>.Fail("Silinmiş kayıtların aktifliği değiştirilemez.", statusCode: 400);

        entity.IsActive = !entity.IsActive;
        entity.UpdatedBy = updatedBy;
        await _unitOfWork.Contacts.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AdminContactDto>.Ok(entity.ToAdminDto());
    }

    public async Task<Result<AdminContactDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Contacts.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminContactDto>.Fail("İletişim mesajı bulunamadı.", statusCode: 404);

        if (!entity.IsDeleted)
            return Result<AdminContactDto>.Fail("Bu kayıt silinmemiş, geri yükleme yapılamaz.", statusCode: 400);

        entity.UpdatedBy = updatedBy;
        await _unitOfWork.Contacts.RestoreAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AdminContactDto>.Ok(entity.ToAdminDto());
    }

    public async Task<Result> DeleteAsync(int id, int? deletedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Contacts.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result.Fail("İletişim mesajı bulunamadı.", statusCode: 404);

        await _unitOfWork.Contacts.SoftDeleteAsync(entity, deletedBy, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"İletişim mesajı silindi. Id: {id}", cancellationToken);

        return Result.Ok();
    }

    public async Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default)
    {
        var summary = await _unitOfWork.Contacts.GetAdminSummaryAsync(cancellationToken);
        return Result<EntitySummaryDto>.Ok(summary);
    }
}
