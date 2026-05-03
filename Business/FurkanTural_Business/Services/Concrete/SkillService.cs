using FurkanTural_Application.DTOs.Skill;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using FurkanTural_Business.Mappers;

namespace FurkanTural_Business.Services.Concrete;

public class SkillService(IUnitOfWork unitOfWork) : ISkillService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<Result<SkillDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Skills.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return Result<SkillDto>.Fail("Yetenek bulunamadı.", statusCode: 404);

        return Result<SkillDto>.Ok(entity.ToDto());
    }

    public async Task<Result<IEnumerable<SkillDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.Skills.GetAllAsync(cancellationToken);
        return Result<IEnumerable<SkillDto>>.Ok(entities.Select(e => e.ToDto()));
    }

    public async Task<PagedResult<SkillDto>> GetAllPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.Skills.GetAllPagedAsync(pageNumber, pageSize, cancellationToken: cancellationToken);
        var total = await _unitOfWork.Skills.CountAsync(cancellationToken: cancellationToken);
        return PagedResult<SkillDto>.Ok(entities.Select(e => e.ToDto()), total, pageNumber, pageSize);
    }

    public async Task<Result<SkillDto>> CreateAsync(CreateSkillDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return Result<SkillDto>.Fail("Yetenek adı boş olamaz.");

        if (dto.Proficiency < 0 || dto.Proficiency > 100)
            return Result<SkillDto>.Fail("Yeterlilik değeri 0 ile 100 arasında olmalıdır.");

        var entity = dto.ToEntity();
        await _unitOfWork.Skills.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<SkillDto>.Ok(entity.ToDto());
    }

    public async Task<Result<SkillDto>> UpdateAsync(UpdateSkillDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Skills.GetByIdAsync(dto.Id, cancellationToken);
        if (entity is null)
            return Result<SkillDto>.Fail("Yetenek bulunamadı.", statusCode: 404);

        if (string.IsNullOrWhiteSpace(dto.Name))
            return Result<SkillDto>.Fail("Yetenek adı boş olamaz.");

        if (dto.Proficiency < 0 || dto.Proficiency > 100)
            return Result<SkillDto>.Fail("Yeterlilik değeri 0 ile 100 arasında olmalıdır.");

        entity.UpdateEntity(dto);
        await _unitOfWork.Skills.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<SkillDto>.Ok(entity.ToDto());
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Skills.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return Result.Fail("Yetenek bulunamadı.", statusCode: 404);

        await _unitOfWork.Skills.SoftDeleteAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}