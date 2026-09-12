using FurkanTural_Application.DTOs.Common;

namespace FurkanTural_Application.Services.Abstract;

public interface IAdminPendingWorkService
{
    Task<AdminPendingWorkDto> GetAsync(string kind, CancellationToken cancellationToken = default);
}
