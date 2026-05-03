using FurkanTural_Domain.Entities;

namespace FurkanTural_Application.Repositories.Abstract;

public interface IBlogRepository : IRepository<Blog>
{
    Task<IEnumerable<Blog>> GetAllForAdminAsync(CancellationToken cancellationToken = default);
}