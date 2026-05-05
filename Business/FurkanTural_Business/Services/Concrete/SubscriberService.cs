using FurkanTural_Application.DTOs.Subscriber;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using FurkanTural_Business.Mappers;

namespace FurkanTural_Business.Services.Concrete;

public class SubscriberService(IUnitOfWork unitOfWork) : ISubscriberService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<Result<SubscriberDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Subscribers.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return Result<SubscriberDto>.Fail("Abone bulunamadı.", statusCode: 404);

        return Result<SubscriberDto>.Ok(entity.ToDto());
    }

    public async Task<Result<IEnumerable<SubscriberDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.Subscribers.GetAllAsync(cancellationToken);
        return Result<IEnumerable<SubscriberDto>>.Ok(entities.Select(e => e.ToDto()));
    }

    public async Task<PagedResult<SubscriberDto>> GetAllPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.Subscribers.GetAllPagedAsync(pageNumber, pageSize, cancellationToken: cancellationToken);
        var total = await _unitOfWork.Subscribers.CountAsync(cancellationToken: cancellationToken);
        return PagedResult<SubscriberDto>.Ok(entities.Select(e => e.ToDto()), total, pageNumber, pageSize);
    }

    public async Task<Result<SubscriberDto>> CreateAsync(CreateSubscriberDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
            return Result<SubscriberDto>.Fail("E-posta adresi boş olamaz.");

        var entity = dto.ToEntity();
        await _unitOfWork.Subscribers.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<SubscriberDto>.Ok(entity.ToDto());
    }

    public async Task<Result<SubscriberDto>> UpdateAsync(UpdateSubscriberDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Subscribers.GetByIdAsync(dto.Id, cancellationToken);
        if (entity is null)
            return Result<SubscriberDto>.Fail("Abone bulunamadı.", statusCode: 404);

        if (string.IsNullOrWhiteSpace(dto.Email))
            return Result<SubscriberDto>.Fail("E-posta adresi boş olamaz.");

        entity.UpdateEntity(dto);
        await _unitOfWork.Subscribers.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<SubscriberDto>.Ok(entity.ToDto());
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Subscribers.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return Result.Fail("Abone bulunamadı.", statusCode: 404);

        await _unitOfWork.Subscribers.SoftDeleteAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    public async Task<Result> SubscribeAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result.Fail("E-posta adresi boş olamaz.");

        var alreadyExists = await _unitOfWork.Subscribers.AnyAsync(x => x.Email == email, cancellationToken);
        if (alreadyExists)
            return Result.Fail("Bu e-posta adresi zaten abone listesinde.");

        var entity = new CreateSubscriberDto { Email = email }.ToEntity();
        await _unitOfWork.Subscribers.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok("Abonelik başarıyla tamamlandı.");
    }

    public async Task<Result> UnsubscribeAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result.Fail("E-posta adresi boş olamaz.");

        var entity = await _unitOfWork.Subscribers.GetAsync(x => x.Email == email, cancellationToken);
        if (entity is null)
            return Result.Fail("Bu e-posta adresi abone listesinde bulunamadı.", statusCode: 404);

        await _unitOfWork.Subscribers.SoftDeleteAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok("Abonelik başarıyla iptal edildi.");
    }

    public async Task<Result<AdminSubscriberDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Subscribers.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminSubscriberDto>.Fail("Abone bulunamadı.", statusCode: 404);

        if (!entity.IsDeleted)
            return Result<AdminSubscriberDto>.Fail("Bu kayıt silinmemiş, geri yükleme yapılamaz.", statusCode: 400);

        entity.UpdatedBy = updatedBy;
        await _unitOfWork.Subscribers.RestoreAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AdminSubscriberDto>.Ok(entity.ToAdminDto());
    }
}