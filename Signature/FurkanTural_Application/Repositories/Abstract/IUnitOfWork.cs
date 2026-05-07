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
    IRepository<Skill> Skills { get; }
    IRepository<Subscriber> Subscribers { get; }
    IRepository<User> Users { get; }
    ILogRepository Logs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}