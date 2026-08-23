using FurkanTural_Domain.Entities.Common;
using FurkanTural_Domain.Entities;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Persistence.Contexts;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FurkanTural_Persistence.Repositories.Concrete;

/// <summary>Özelleşmiş repo'lar kendi alanlarında, genel olanlar tür anahtarlı bir sözlükte tutulur. Bu yüzden GetRepository&lt;Blog&gt; ile Blogs iki ayrı nesne döndürür; ikisi de aynı bağlamı sardığı için durum ikilenmez, çünkü saklanan tek durum DbContext'in kendisindedir.</summary>
public class UnitOfWork(FurkanTuralDbContext context) : IUnitOfWork
{
    private readonly Dictionary<Type, object> _repos = new();
    private IBlogRepository? _blogs;
    private IRepository<BlogImage>? _blogImages;
    private ILogRepository? _logs;
    private IChatMessageRepository? _chatMessages;
    private IUserRepository? _users;

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
    public IUserRepository Users => _users ??= new UserRepository(context);
    public IRepository<Contact> Contacts => GetRepository<Contact>();
    public IRepository<ContactTemplate> ContactTemplates => GetRepository<ContactTemplate>();
    public IRepository<Status> Statuses => GetRepository<Status>();
    public IRepository<UserFriend> UserFriends => GetRepository<UserFriend>();
    public IChatMessageRepository ChatMessages => _chatMessages ??= new ChatMessageRepository(context);
    public IRepository<CallLog> CallLogs => GetRepository<CallLog>();
    public IRepository<Report> Reports => GetRepository<Report>();
    public IRepository<CallPolicy> CallPolicies => GetRepository<CallPolicy>();
    public IRepository<PushSubscription> PushSubscriptions => GetRepository<PushSubscription>();
    public IRepository<AccountActivation> AccountActivations => GetRepository<AccountActivation>();

    /// <summary>Her yazmanın tek boğazı burasıdır, bu yüzden veri tabanı kısıtlarının çevirisi de burada durur. Kayıt akışlarındaki "önce ara, yoksa ekle" deseni yarışı kapatamaz: iki istek aynı anda aramadan geçip ikisi de yazmaya gidebilir. Yarışı uygulama kodunda önlemenin yolu yoktur, son sözü indeks söyler — buradaki iş o sözü çağıranın anlayabileceği bir istisnaya çevirmek, böylece dışarıya 500 yerine anlamlı bir yanıt dönebilmektir.<para>Yalnızca <see cref="PersistenceConflictTranslator"/>'ın tanıdığı numaralar çevrilir; gerisi <c>throw;</c> ile olduğu gibi, yığın izi bozulmadan yükselir.</para><para>Çeviri değişiklik izleyicisine dokunmaz, başarısız satır <c>Added</c> durumunda kalır. Bu istisnayı yakalayıp aynı kapsamda yazmaya devam eden bir çağıran o satırı yeniden göndermiş olur; dolayısıyla istisna yutulmamalı, isteği sonlandırmalıdır.</para></summary>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sql)
        {
            var conflict = PersistenceConflictTranslator.Translate(sql.Number, sql.Message, ex);
            if (conflict is null) throw;
            throw conflict;
        }
    }
}
