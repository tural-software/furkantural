using System.Linq.Expressions;
using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Application.Repositories.Abstract;

public interface IReadRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<T?> GetAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllPagedAsync(int pageNumber, int pageSize, Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
}