using FurkanTural_Domain.Entities;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace FurkanTural_Persistence.Repositories.Concrete;

public class BlogRepository(FurkanTuralDbContext context) : Repository<Blog>(context), IBlogRepository
{
    public async Task<IEnumerable<Blog>> GetAllForAdminAsync(CancellationToken cancellationToken = default)
        => await _dbSet.IgnoreQueryFilters().AsNoTracking().ToListAsync(cancellationToken);
}