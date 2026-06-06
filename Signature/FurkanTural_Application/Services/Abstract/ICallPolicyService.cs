using FurkanTural_Application.DTOs.Call;
using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

public interface ICallPolicyService
{
    /// <summary>
    /// Kullanıcının efektif video politikası. v1: küresel politika.
    /// İleride abonelik/kademe (tier) bazlı dallanma buraya eklenir — uç/istemci sözleşmesi değişmez.
    /// </summary>
    Task<VideoPolicyDto> GetEffectiveForUserAsync(int userId, CancellationToken cancellationToken = default);

    // ── Admin ──
    Task<Result<AdminCallPolicyDto>> GetForAdminAsync(CancellationToken cancellationToken = default);
    Task<Result<AdminCallPolicyDto>> UpdateAsync(UpdateCallPolicyDto dto, int? updatedBy, CancellationToken cancellationToken = default);
}
