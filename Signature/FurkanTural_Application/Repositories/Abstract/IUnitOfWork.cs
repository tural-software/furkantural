using FurkanTural_Domain.Entities;
using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Application.Repositories.Abstract;

public interface IUnitOfWork
{
    IRepository<T> GetRepository<T>() where T : BaseEntity;

    IRepository<Blog> Blogs { get; }
    IRepository<BlogImage> BlogImages { get; }
    IRepository<Education> Educations { get; }
    IRepository<Experience> Experiences { get; }
    IRepository<Music> Musics { get; }
    IRepository<MusicImage> MusicImages { get; }
    IRepository<Project> Projects { get; }
    IRepository<ProjectImage> ProjectImages { get; }
    IRepository<Role> Roles { get; }
    IRepository<Skill> Skills { get; }
    IRepository<Subscriber> Subscribers { get; }
    IRepository<User> Users { get; }
    IRepository<Contact> Contacts { get; }
    IRepository<ContactTemplate> ContactTemplates { get; }
    IRepository<Status> Statuses { get; }
    IRepository<UserFriend> UserFriends { get; }
    IChatMessageRepository ChatMessages { get; }
    IRepository<CallLog> CallLogs { get; }
    IRepository<Report> Reports { get; }
    IRepository<CallPolicy> CallPolicies { get; }
    IRepository<PushSubscription> PushSubscriptions { get; }
    ILogRepository Logs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}