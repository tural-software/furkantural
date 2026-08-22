using FurkanTural_Chat.Models.Auth;
using FurkanTural_Chat.Models.Wrappers;

namespace FurkanTural_Chat.Services;

public interface IChatAuthApiClient
{
    Task<ApiResult<AuthResultModel>> LoginAsync(LoginRequestModel request, CancellationToken cancellationToken = default);
    Task<ApiResult<AuthResultModel>> RegisterAsync(RegisterRequestModel request, CancellationToken cancellationToken = default);
}