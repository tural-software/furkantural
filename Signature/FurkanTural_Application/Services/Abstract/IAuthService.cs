using FurkanTural_Application.DTOs.Auth;
using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

public interface IAuthService
{
    Task<Result<LoginResultDto>> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default);
    Task<Result<LoginResultDto>> GenerateAppTokenAsync(AppTokenRequestDto dto, CancellationToken cancellationToken = default);
}
