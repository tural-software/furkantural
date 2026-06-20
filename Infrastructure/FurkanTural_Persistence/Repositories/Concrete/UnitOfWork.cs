using FurkanTural_Domain.Entities.Common;
using FurkanTural_Domain.Entities;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Persistence.Contexts;

namespace FurkanTural_Persistence.Repositories.Concrete;

public class UnitOfWork(FurkanTuralDbContext context) : IUnitOfWork
{
    private readonly Dictionary<Type, object> _repos = new();
    private IBlogRepository? _blogs;
    private IRepository<BlogImage>? _blogImages;
    private ILogRepository? _logs;
    private IChatMessageRepository? _chatMessages;

    public IRepository<T> GetRepository<T>() where T : BaseEntity
    {
        if (!_repos.TryGetValue(typeof(T), out var repo))
            _repos[typeof(T)] = repo = new Repository<T>(context);
        return (IRepository<T>)repo;
    }

    public IBlogRepository Blogs => _blogs ??= new BlogRepository(context);
    public IRepository<BlogImage> BlogImages => _blogImages ??= new Repository<BlogImage>(context);
    public IRepository<Category> Categories => GetRepository<Category>();
    public ILogRepository Logs => _logs ??= new LogRepository(context);
    public IRepository<Education> Educations => GetRepository<Education>();
    public IRepository<Experience> Experiences => GetRepository<Experience>();
    public IRepository<Music> Musics => GetRepository<Music>();
    public IRepository<MusicImage> MusicImages => GetRepository<MusicImage>();
    public IRepository<Project> Projects => GetRepository<Project>();
    public IRepository<ProjectImage> ProjectImages => GetRepository<ProjectImage>();
    public IRepository<Role> Roles => GetRepository<Role>();
    public IRepository<Skill> Skills => GetRepository<Skill>();
    public IRepository<Subscriber> Subscribers => GetRepository<Subscriber>();
    public IRepository<User> Users => GetRepository<User>();
    public IRepository<Contact> Contacts => GetRepository<Contact>();
    public IRepository<ContactTemplate> ContactTemplates => GetRepository<ContactTemplate>();
    public IRepository<Status> Statuses => GetRepository<Status>();
    public IRepository<UserFriend> UserFriends => GetRepository<UserFriend>();
    public IChatMessageRepository ChatMessages => _chatMessages ??= new ChatMessageRepository(context);
    public IRepository<CallLog> CallLogs => GetRepository<CallLog>();
    public IRepository<Report> Reports => GetRepository<Report>();
    public IRepository<CallPolicy> CallPolicies => GetRepository<CallPolicy>();
    public IRepository<PushSubscription> PushSubscriptions => GetRepository<PushSubscription>();

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => context.SaveChangesAsync(cancellationToken);
}