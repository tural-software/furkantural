using FurkanTural_Application.DTOs.User;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using FurkanTural_Business.Mappers;

namespace FurkanTural_Business.Services.Concrete;

public class UserService(IUnitOfWork unitOfWork, IEncryptionService encryptionService) : IUserService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IEncryptionService _encryptionService = encryptionService;

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

        var encryptResult = _encryptionService.Encrypt(dto.Password);
        if (encryptResult.IsFailure)
            return Result<UserDto>.Fail(encryptResult.Errors, encryptResult.InternalMessage);

        var entity = dto.ToEntity();
        entity.Password = encryptResult.Data;

        await _unitOfWork.Users.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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

        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            var encryptResult = _encryptionService.Encrypt(dto.Password);
            if (encryptResult.IsFailure)
                return Result<UserDto>.Fail(encryptResult.Errors, encryptResult.InternalMessage);

            entity.Password = encryptResult.Data;
        }

        await _unitOfWork.Users.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<UserDto>.Ok(entity.ToDto());
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Users.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return Result.Fail("Kullanıcı bulunamadı.", statusCode: 404);

        await _unitOfWork.Users.SoftDeleteAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}