using FurkanTural_Admin.Models.Auth;
using FurkanTural_Admin.Models.Wrappers;

namespace FurkanTural_Admin.Services;

public interface IAuthApiClient
{
    Task<ApiResult<LoginResultModel>> LoginAsync(LoginRequestModel request, CancellationToken cancellationToken = default);
}