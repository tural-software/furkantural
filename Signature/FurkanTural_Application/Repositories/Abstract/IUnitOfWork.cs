using FurkanTural_Domain.Entities;
using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Application.Repositories.Abstract;

/// <summary>Tüm repo'ları tek bir veri tabanı bağlamında toplar ve kaydetme anını sahiplenir. Repo'lar örnek başına önbelleklenir, bu yüzden buradan alınan repo'lar aynı değişiklik izleyicisini paylaşır: farklı repo'lara yazılan değişiklikler tek SaveChangesAsync ile birlikte kaydedilir. Açık bir transaction arayüzü yoktur; atomik olan tek şey bir SaveChangesAsync çağrısıdır, iki ayrı çağrı iki ayrı transaction demektir — blog kaydı ile kategori bağlarının ayrı kaydedilmesi buna örnektir, ikincisi başarısız olursa yazı kategorisiz kalır.<para>Adı verilmiş özellikler ile GetRepository&lt;T&gt; aynı şey değildir: Blogs, ChatMessages, Logs ve Users özelleşmiş sözleşmeler döndürür, GetRepository&lt;Blog&gt; ise yalnızca genel repo'yu verir ve blog'a özgü metotları içermez.</para></summary>
public interface IUnitOfWork
{
    IRepository<T> GetRepository<T>() where T : BaseEntity;
    IBlogRepository Blogs { get; }
    IRepository<BlogImage> BlogImages { get; }
    IRepository<Category> Categories { get; }
    IRepository<Education> Educations { get; }
    IRepository<Experience> Experiences { get; }
    IRepository<Music> Musics { get; }
    IRepository<MusicImage> MusicImages { get; }
    IRepository<Project> Projects { get; }
    IRepository<ProjectImage> ProjectImages { get; }
    IRepository<Role> Roles { get; }
    IRepository<Skill> Skills { get; }
    IRepository<Subscriber> Subscribers { get; }
    IUserRepository Users { get; }
    IRepository<Contact> Contacts { get; }
    IRepository<MailTemplateType> MailTemplateTypes { get; }
    IRepository<MailTemplate> MailTemplates { get; }
    IRepository<Status> Statuses { get; }
    IRepository<UserFriend> UserFriends { get; }
    IChatMessageRepository ChatMessages { get; }
    IRepository<CallLog> CallLogs { get; }
    IRepository<Report> Reports { get; }
    IRepository<CallPolicy> CallPolicies { get; }
    IRepository<PushSubscription> PushSubscriptions { get; }
    IRepository<AccountActivation> AccountActivations { get; }
    ILogRepository Logs { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
