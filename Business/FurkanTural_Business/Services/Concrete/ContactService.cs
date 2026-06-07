using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.DTOs.Contact;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using FurkanTural_Business.Helpers;
using FurkanTural_Business.Mappers;
using Microsoft.Extensions.Configuration;

namespace FurkanTural_Business.Services.Concrete;

public class ContactService(
    IUnitOfWork unitOfWork,
    IEmailService emailService,
    IConfiguration configuration,
    ActivityLogger activityLogger,
    ITurnstileVerifier turnstileVerifier,
    IClock clock) : IContactService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IEmailService _emailService = emailService;
    private readonly IConfiguration _configuration = configuration;
    private readonly ActivityLogger _activityLogger = activityLogger;
    private readonly ITurnstileVerifier _turnstileVerifier = turnstileVerifier;
    private readonly IClock _clock = clock;

    public async Task<Result> SubmitAsync(SubmitContactDto dto, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default)
    {
        // Turnstile doğrulaması (paylaşılan doğrulayıcı; SecretKey yapılandırılmamışsa atlar)
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

        // Send email notifications
        await SendEmailsAsync(dto, ipAddress, userAgent, cancellationToken);

        return Result.Ok();
    }

    private async Task SendEmailsAsync(SubmitContactDto dto, string? ipAddress, string? userAgent, CancellationToken ct)
    {
        var ownerEmail   = _configuration["Contact:OwnerEmail"]   ?? "furkanturalofficial@outlook.com";
        var formPageUrl  = _configuration["Contact:FormPageUrl"]  ?? "";
        var linkedInUrl  = _configuration["Contact:LinkedInUrl"]  ?? "";
        var gitHubUrl    = _configuration["Contact:GitHubUrl"]    ?? "";
        var instagramUrl = _configuration["Contact:InstagramUrl"] ?? "";
        var contactEmail = _configuration["Contact:ContactEmail"] ?? "";
        var now = _clock.UtcNow;

        var templates = await _unitOfWork.ContactTemplates.GetAllAsync(ct);

        // Owner template
        var ownerTemplate = templates.FirstOrDefault(t => t.TemplateType == "Owner" && t.IsActive);
        if (ownerTemplate?.HtmlContent is not null)
        {
            var ownerBody = ReplacePlaceholders(ownerTemplate.HtmlContent, dto.Name, dto.Email, dto.Message,
                now, ipAddress, userAgent, formPageUrl, linkedInUrl, gitHubUrl, instagramUrl, contactEmail);
            try { await _emailService.SendAsync(ownerEmail, $"Yeni İletişim Mesajı - {dto.Name}", ownerBody, ct); }
            catch { /* log, don't fail the request */ }
        }

        // User template
        var userTemplate = templates.FirstOrDefault(t => t.TemplateType == "User" && t.IsActive);
        if (userTemplate?.HtmlContent is not null && !string.IsNullOrWhiteSpace(dto.Email))
        {
            var userBody = ReplacePlaceholders(userTemplate.HtmlContent, dto.Name, dto.Email, dto.Message,
                now, ipAddress, userAgent, formPageUrl, linkedInUrl, gitHubUrl, instagramUrl, contactEmail);
            try { await _emailService.SendAsync(dto.Email!, "Mesajınız Alındı - Furkan Tural", userBody, ct); }
            catch { /* log, don't fail the request */ }
        }
    }

    private static string ReplacePlaceholders(
        string html, string? name, string? email, string? message, DateTime date,
        string? ip, string? browser, string? formPageUrl,
        string? linkedInUrl, string? gitHubUrl, string? instagramUrl, string? contactEmail)
        => html
            .Replace("{{FullName}}",     name ?? "")
            .Replace("{{Email}}",        email ?? "")
            .Replace("{{Message}}",      message ?? "")
            .Replace("{{CreatedAt}}",    date.ToString("dd.MM.yyyy HH:mm"))
            .Replace("{{IpAddress}}",    ip ?? "")
            .Replace("{{Browser}}",      browser ?? "")
            .Replace("{{FormPageUrl}}",  formPageUrl ?? "")
            .Replace("{{LinkedInUrl}}",  linkedInUrl ?? "")
            .Replace("{{GitHubUrl}}",    gitHubUrl ?? "")
            .Replace("{{InstagramUrl}}", instagramUrl ?? "")
            .Replace("{{ContactEmail}}", contactEmail ?? "")
            .Replace("{{CurrentYear}}",  date.Year.ToString());

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

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Contacts.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result.Fail("İletişim mesajı bulunamadı.", statusCode: 404);

        await _unitOfWork.Contacts.SoftDeleteAsync(entity, cancellationToken);
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
