using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

public interface IService<TDto, TCreateDto, TUpdateDto>
{
    Task<Result<TDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<TDto>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<TDto>> GetAllPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<Result<TDto>> CreateAsync(TCreateDto dto, CancellationToken cancellationToken = default);
    Task<Result<TDto>> UpdateAsync(TUpdateDto dto, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);
}