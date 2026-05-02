using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Domain.Entities;
using FurkanTural_Domain.Entities.Common;
using FurkanTural_Persistence.Contexts;

namespace FurkanTural_Persistence.Repositories.Concrete;

public class UnitOfWork : IUnitOfWork
{
    private readonly FurkanTuralDbContext _context;
    private readonly Dictionary<Type, object> _repos = new();
    private ILogRepository? _logs;

    public UnitOfWork(FurkanTuralDbContext context)
    {
        _context = context;
    }

    public IRepository<T> GetRepository<T>() where T : BaseEntity
    {
        if (!_repos.TryGetValue(typeof(T), out var repo))
            _repos[typeof(T)] = repo = new Repository<T>(_context);
        return (IRepository<T>)repo;
    }

    public IRepository<Blog> Blogs => GetRepository<Blog>();
    public IRepository<BlogImage> BlogImages => GetRepository<BlogImage>();
    public IRepository<Education> Educations => GetRepository<Education>();
    public IRepository<Music> Musics => GetRepository<Music>();
    public IRepository<MusicImage> MusicImages => GetRepository<MusicImage>();
    public IRepository<Skill> Skills => GetRepository<Skill>();
    public IRepository<Subscriber> Subscribers => GetRepository<Subscriber>();
    public IRepository<User> Users => GetRepository<User>();
    public ILogRepository Logs => _logs ??= new LogRepository(_context);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
