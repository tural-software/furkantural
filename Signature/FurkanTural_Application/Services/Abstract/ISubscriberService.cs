using FurkanTural_Application.DTOs.Subscriber;
using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

public interface ISubscriberService : IService<SubscriberDto, CreateSubscriberDto, UpdateSubscriberDto>
{
    Task<Result> SubscribeAsync(string email, CancellationToken cancellationToken = default);
    Task<Result> UnsubscribeAsync(string email, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<AdminSubscriberDto>>> GetAllForAdminAsync(CancellationToken cancellationToken = default);
    Task<Result<AdminSubscriberDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<AdminSubscriberDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<AdminSubscriberDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
}