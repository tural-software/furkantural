using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Application.Repositories.Abstract;

public interface IRepository<T> : IReadRepository<T>, IWriteRepository<T> where T : BaseEntity {  }