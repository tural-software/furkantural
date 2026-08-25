using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.DTOs.User;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using FurkanTural_Business.Helpers;
using FurkanTural_Business.Mappers;
using FurkanTural_Domain.Constants;

namespace FurkanTural_Business.Services.Concrete;

public class UserService(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher, ActivityLogger activityLogger, IUserFriendService userFriendService, IClock clock) : IUserService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;
    private readonly ActivityLogger _activityLogger = activityLogger;
    private readonly IUserFriendService _userFriendService = userFriendService;
    private readonly IClock _clock = clock;

    public async Task<Result<UserDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Users.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return Result<UserDto>.Fail("Kullanıcı bulunamadı.", statusCode: 404);

        return Result<UserDto>.Ok(entity.ToDto());
    }

    public async Task<Result<IEnumerable<UserDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.Users.GetAllAsync(cancellationToken);
        return Result<IEnumerable<UserDto>>.Ok(entities.Select(e => e.ToDto()));
    }

    public async Task<PagedResult<UserDto>> GetAllPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.Users.GetAllPagedAsync(pageNumber, pageSize, cancellationToken: cancellationToken);
        var total = await _unitOfWork.Users.CountAsync(cancellationToken: cancellationToken);
        return PagedResult<UserDto>.Ok(entities.Select(e => e.ToDto()), total, pageNumber, pageSize);
    }

    public async Task<Result<UserDto>> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username))
            return Result<UserDto>.Fail("Kullanıcı adı boş olamaz.");

        var entity = await _unitOfWork.Users.GetAsync(x => x.Username == username, cancellationToken);
        if (entity is null)
            return Result<UserDto>.Fail("Kullanıcı bulunamadı.", statusCode: 404);

        return Result<UserDto>.Ok(entity.ToDto());
    }

    public async Task<Result<UserDto>> CreateAsync(CreateUserDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Username))
            return Result<UserDto>.Fail("Kullanıcı adı boş olamaz.");

        if (string.IsNullOrWhiteSpace(dto.Password))
            return Result<UserDto>.Fail("Şifre boş olamaz.");

        var usernameExists = await _unitOfWork.Users.AnyAsync(x => x.Username == dto.Username, cancellationToken);
        if (usernameExists)
            return Result<UserDto>.Fail("Bu kullanıcı adı zaten kullanılıyor.");

        var entity = dto.ToEntity();
        entity.Password = _passwordHasher.Hash(dto.Password);

        await _unitOfWork.Users.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Kullanıcı oluşturuldu. Id: {entity.Id}", cancellationToken);

        return Result<UserDto>.Ok(entity.ToDto());
    }

    public async Task<Result<UserDto>> UpdateAsync(UpdateUserDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Users.GetByIdAsync(dto.Id, cancellationToken);
        if (entity is null)
            return Result<UserDto>.Fail("Kullanıcı bulunamadı.", statusCode: 404);

        if (string.IsNullOrWhiteSpace(dto.Username))
            return Result<UserDto>.Fail("Kullanıcı adı boş olamaz.");

        var usernameExists = await _unitOfWork.Users.AnyAsync(x => x.Username == dto.Username && x.Id != dto.Id, cancellationToken);
        if (usernameExists)
            return Result<UserDto>.Fail("Bu kullanıcı adı zaten kullanılıyor.");

        entity.Username = dto.Username;
        entity.RoleId = dto.RoleId;
        entity.Email = dto.Email;
        entity.DisplayName = dto.DisplayName;
        entity.AvatarUrl = dto.AvatarUrl;
        entity.UpdatedBy = dto.UpdatedBy;

        if (!string.IsNullOrWhiteSpace(dto.Password))
            entity.Password = _passwordHasher.Hash(dto.Password);

        await _unitOfWork.Users.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Kullanıcı güncellendi. Id: {entity.Id}", cancellationToken);

        return Result<UserDto>.Ok(entity.ToDto());
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Users.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return Result.Fail("Kullanıcı bulunamadı.", statusCode: 404);

        await _unitOfWork.Users.SoftDeleteAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Kullanıcı silindi. Id: {id}", cancellationToken);

        return Result.Ok();
    }

    public async Task<Result<IEnumerable<AdminUserDto>>> GetAllForAdminAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.Users.GetAllForAdminAsync(cancellationToken);
        return Result<IEnumerable<AdminUserDto>>.Ok(entities.Select(e => e.ToAdminDto()));
    }

    public async Task<Result<AdminUserDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Users.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminUserDto>.Fail("Kullanıcı bulunamadı.", statusCode: 404);

        return Result<AdminUserDto>.Ok(entity.ToAdminDto());
    }

    public async Task<Result<AdminUserDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Users.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminUserDto>.Fail("Kullanıcı bulunamadı.", statusCode: 404);

        if (entity.IsDeleted)
            return Result<AdminUserDto>.Fail("Silinmiş kayıtların aktifliği değiştirilemez.", statusCode: 400);

        entity.IsActive = !entity.IsActive;
        entity.UpdatedBy = updatedBy;

        await _unitOfWork.Users.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Kullanıcı aktiflik durumu değiştirildi. Id: {id}, Yeni durum: {entity.IsActive}", cancellationToken);

        return Result<AdminUserDto>.Ok(entity.ToAdminDto());
    }

    public async Task<Result<AdminUserDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Users.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminUserDto>.Fail("Kullanıcı bulunamadı.", statusCode: 404);

        if (!entity.IsDeleted)
            return Result<AdminUserDto>.Fail("Bu kayıt silinmemiş, geri yükleme yapılamaz.", statusCode: 400);

        entity.UpdatedBy = updatedBy;
        await _unitOfWork.Users.RestoreAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Kullanıcı geri yüklendi. Id: {id}", cancellationToken);

        return Result<AdminUserDto>.Ok(entity.ToAdminDto());
    }

    public async Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default)
    {
        var summary = await _unitOfWork.Users.GetAdminSummaryAsync(cancellationToken);
        return Result<EntitySummaryDto>.Ok(summary);
    }

    public async Task<Result<UserDto>> SeedAdminAsync(string? username, string? password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username))
            return Result<UserDto>.Fail("Kullanıcı adı boş olamaz.");

        if (string.IsNullOrWhiteSpace(password))
            return Result<UserDto>.Fail("Şifre boş olamaz.");

        var anyUser = await _unitOfWork.Users.AnyAsync(_ => true, cancellationToken);
        if (anyUser)
            return Result<UserDto>.Fail("Sistemde zaten kullanıcı mevcut. Seed işlemi yapılamaz.", statusCode: 409);

        var dto = new CreateUserDto { Username = username, Password = password, RoleId = 1 };
        var entity = dto.ToEntity();
        entity.Password = _passwordHasher.Hash(password);

        await _unitOfWork.Users.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"İlk admin kullanıcısı oluşturuldu. Kullanıcı adı: {entity.Username}", cancellationToken);

        return Result<UserDto>.Ok(entity.ToDto());
    }

    public async Task<Result<IEnumerable<UserSearchResultDto>>> SearchAsync(string query, int currentUserId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
            return Result<IEnumerable<UserSearchResultDto>>.Fail("Arama için en az 2 karakter girin.");

        var q = query.Trim();
        var matches = await _unitOfWork.Users.GetAllAsync(
            x => x.Id != currentUserId &&
                ((x.Username != null && x.Username.Contains(q)) ||
                 x.Email == q ||
                 (x.DisplayName != null && x.DisplayName.Contains(q))),
            cancellationToken);

        var results = new List<UserSearchResultDto>();
        foreach (var u in matches.Take(40))
        {
            if (await _userFriendService.IsBlockedBetweenAsync(currentUserId, u.Id, cancellationToken))
                continue;

            results.Add(new UserSearchResultDto
            {
                Id = u.Id,
                Username = u.Username,
                DisplayName = u.DisplayName,
                AvatarUrl = u.AvatarUrl
            });

            if (results.Count >= 20)
                break;
        }

        return Result<IEnumerable<UserSearchResultDto>>.Ok(results);
    }

    public async Task<Result<UserDto>> UpdateAvatarAsync(int userId, string fileName, int? updatedBy, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return Result<UserDto>.Fail("Avatar dosyası gerekli.");

        var entity = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (entity is null)
            return Result<UserDto>.Fail("Kullanıcı bulunamadı.", statusCode: 404);

        entity.AvatarUrl = fileName;
        entity.UpdatedBy = updatedBy;

        await _unitOfWork.Users.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Kullanıcı avatarı güncellendi. Id: {userId}", cancellationToken);

        return Result<UserDto>.Ok(entity.ToDto());
    }

    public async Task<DateTime> UpdateLastSeenAsync(int userId, CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var entity = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (entity is null)
            return now;

        entity.LastSeenAt = now;
        await _unitOfWork.Users.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return now;
    }

    public async Task<Result> AcceptAgreementAsync(int userId, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (entity is null)
            return Result.Fail("Kullanıcı bulunamadı.", statusCode: 404);

        entity.MembershipAgreementAcceptedAt = _clock.UtcNow;
        entity.MembershipAgreementVersion = AgreementDefinitions.CurrentVersion;
        entity.UpdatedBy = userId;

        await _unitOfWork.Users.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Üyelik sözleşmesi kabul edildi. Id: {userId}, Sürüm: {AgreementDefinitions.CurrentVersion}", cancellationToken);

        return Result.Ok();
    }

    public async Task<Result> DeactivateMyAccountAsync(int userId, string? password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(password))
            return Result.Fail("Hesabınızı kapatmak için parolanızı girmeniz gerekiyor.");

        var entity = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (entity is null)
            return Result.Fail("Kullanıcı bulunamadı.", $"Hesap kapatma reddedildi: #{userId} etkin değil ya da silinmiş.", 404);

        if (string.IsNullOrWhiteSpace(entity.Password) || !_passwordHasher.Verify(password, entity.Password))
            return Result.Fail("Parola hatalı.", $"Hesap kapatma reddedildi: #{userId} parola doğrulanamadı.", 401);

        entity.IsActive = false;
        entity.UpdatedBy = userId;

        await _unitOfWork.Users.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Kullanıcı kendi hesabını kapattı. Id: {userId}", cancellationToken);

        return Result.Ok("Hesabınız kapatıldı.");
    }
}
