using FurkanTural_Application.DTOs.Education;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using FurkanTural_Business.Mappers;

namespace FurkanTural_Business.Services.Concrete;

public class EducationService(IUnitOfWork unitOfWork) : IEducationService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<Result<EducationDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Educations.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return Result<EducationDto>.Fail("Eğitim bilgisi bulunamadı.", statusCode: 404);

        return Result<EducationDto>.Ok(entity.ToDto());
    }

    public async Task<Result<IEnumerable<EducationDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.Educations.GetAllAsync(cancellationToken);
        return Result<IEnumerable<EducationDto>>.Ok(entities.Select(e => e.ToDto()));
    }

    public async Task<PagedResult<EducationDto>> GetAllPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.Educations.GetAllPagedAsync(pageNumber, pageSize, cancellationToken: cancellationToken);
        var total = await _unitOfWork.Educations.CountAsync(cancellationToken: cancellationToken);
        return PagedResult<EducationDto>.Ok(entities.Select(e => e.ToDto()), total, pageNumber, pageSize);
    }

    public async Task<Result<EducationDto>> CreateAsync(CreateEducationDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Institution))
            return Result<EducationDto>.Fail("Kurum adı boş olamaz.");

        if (string.IsNullOrWhiteSpace(dto.Degree))
            return Result<EducationDto>.Fail("Derece bilgisi boş olamaz.");

        if (dto.StartDate.HasValue && dto.EndDate.HasValue && dto.EndDate < dto.StartDate)
            return Result<EducationDto>.Fail("Bitiş tarihi başlangıç tarihinden önce olamaz.");

        var entity = dto.ToEntity();
        await _unitOfWork.Educations.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<EducationDto>.Ok(entity.ToDto());
    }

    public async Task<Result<EducationDto>> UpdateAsync(UpdateEducationDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Educations.GetByIdAsync(dto.Id, cancellationToken);
        if (entity is null)
            return Result<EducationDto>.Fail("Eğitim bilgisi bulunamadı.", statusCode: 404);

        if (string.IsNullOrWhiteSpace(dto.Institution))
            return Result<EducationDto>.Fail("Kurum adı boş olamaz.");

        if (string.IsNullOrWhiteSpace(dto.Degree))
            return Result<EducationDto>.Fail("Derece bilgisi boş olamaz.");

        if (dto.StartDate.HasValue && dto.EndDate.HasValue && dto.EndDate < dto.StartDate)
            return Result<EducationDto>.Fail("Bitiş tarihi başlangıç tarihinden önce olamaz.");

        entity.UpdateEntity(dto);
        await _unitOfWork.Educations.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<EducationDto>.Ok(entity.ToDto());
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Educations.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return Result.Fail("Eğitim bilgisi bulunamadı.", statusCode: 404);

        await _unitOfWork.Educations.SoftDeleteAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}