using FurkanTural_Admin.Models.Common;

namespace FurkanTural_Admin.Services;

public interface IAdminDashboardClient
{
    Task<AdminDashboardModel?> GetAsync(int windowDays, string token, CancellationToken ct = default);
}
