using FurkanTural_Admin.Models.Call;

namespace FurkanTural_Admin.Services;

public interface ICallPolicyApiClient
{
    Task<CallPolicyFormDto?> GetAsync(string token, CancellationToken ct = default);
    Task<bool> UpdateAsync(CallPolicyFormDto dto, string token, CancellationToken ct = default);
}