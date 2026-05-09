using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.DTOs.Log;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using FurkanTural_Business.Mappers;

namespace FurkanTural_Business.Services.Concrete;

public class LogService(IUnitOfWork unitOfWork) : ILogService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<Result<LogDto>> CreateAsync(CreateLogDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Message))
            return Result<LogDto>.Fail("Log mesajı boş olamaz.");

        var entity = dto.ToEntity();
        await _unitOfWork.Logs.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<LogDto>.Ok(entity.ToDto());
    }

    public async Task<Result<LogDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Logs.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return Result<LogDto>.Fail("Log bulunamadı.", statusCode: 404);

        return Result<LogDto>.Ok(entity.ToDto());
    }

    public async Task<Result<IEnumerable<LogDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.Logs.GetAllAsync(cancellationToken: cancellationToken);
        return Result<IEnumerable<LogDto>>.Ok(entities.Select(e => e.ToDto()));
    }

    public async Task<PagedResult<LogDto>> GetAllPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.Logs.GetAllPagedAsync(pageNumber, pageSize, cancellationToken: cancellationToken);
        var total = await _unitOfWork.Logs.CountAsync(cancellationToken: cancellationToken);
        return PagedResult<LogDto>.Ok(entities.Select(e => e.ToDto()), total, pageNumber, pageSize);
    }

    public async Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default)
    {
        var summary = await _unitOfWork.Logs.GetAdminSummaryAsync(cancellationToken);
        return Result<EntitySummaryDto>.Ok(summary);
    }
}