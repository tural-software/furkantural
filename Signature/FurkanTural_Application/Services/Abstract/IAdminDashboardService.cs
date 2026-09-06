using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

public interface IAdminDashboardService
{
    Task<Result<AdminDashboardDto>> GetAsync(DateTime today, int windowDays, CancellationToken cancellationToken = default);
}
