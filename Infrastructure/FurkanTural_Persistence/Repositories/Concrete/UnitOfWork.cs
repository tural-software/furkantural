using FurkanTural_Domain.Entities.Common;
using FurkanTural_Domain.Entities;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Persistence.Contexts;

namespace FurkanTural_Persistence.Repositories.Concrete;

public class UnitOfWork(FurkanTuralDbContext context) : IUnitOfWork
{
    private readonly Dictionary<Type, object> _repos = new();
    private IRepository<Blog>? _blogs;
    private IRepository<BlogImage>? _blogImages;
    private ILogRepository? _logs;

    public IRepository<T> GetRepository<T>() where T : BaseEntity
    {
        if (!_repos.TryGetValue(typeof(T), out var repo))
            _repos[typeof(T)] = repo = new Repository<T>(context);
        return (IRepository<T>)repo;
    }

    public IRepository<Blog> Blogs => _blogs ??= new Repository<Blog>(context);
    public IRepository<BlogImage> BlogImages => _blogImages ??= new Repository<BlogImage>(context);
    public ILogRepository Logs => _logs ??= new LogRepository(context);
    public IRepository<Education> Educations => GetRepository<Education>();
    public IRepository<Music> Musics => GetRepository<Music>();
    public IRepository<MusicImage> MusicImages => GetRepository<MusicImage>();
    public IRepository<Skill> Skills => GetRepository<Skill>();
    public IRepository<Subscriber> Subscribers => GetRepository<Subscriber>();
    public IRepository<User> Users => GetRepository<User>();

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => context.SaveChangesAsync(cancellationToken);
}