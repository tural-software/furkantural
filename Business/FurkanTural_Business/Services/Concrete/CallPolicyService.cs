using FurkanTural_Application.DTOs.Call;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using FurkanTural_Business.Mappers;
using FurkanTural_Domain.Constants;
using FurkanTural_Domain.Entities;

namespace FurkanTural_Business.Services.Concrete;

public class CallPolicyService(IUnitOfWork unitOfWork) : ICallPolicyService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    private const int PolicyId = 1; // tek küresel satır

    public async Task<VideoPolicyDto> GetEffectiveForUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        // v1: küresel politika herkese uygulanır. (Tier dikişi: ileride userId'ye göre dallanır.)
        var entity = await _unitOfWork.CallPolicies.GetByIdAsync(PolicyId, cancellationToken);
        return entity?.ToVideoPolicyDto() ?? DefaultVideoPolicy();
    }

    public async Task<Result<AdminCallPolicyDto>> GetForAdminAsync(CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.CallPolicies.GetByIdForAdminAsync(PolicyId, cancellationToken);
        if (entity is null)
            return Result<AdminCallPolicyDto>.Fail("Arama politikası bulunamadı.", statusCode: 404);

        return Result<AdminCallPolicyDto>.Ok(entity.ToAdminDto());
    }

    public async Task<Result<AdminCallPolicyDto>> UpdateAsync(UpdateCallPolicyDto dto, int? updatedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.CallPolicies.GetByIdForAdminAsync(PolicyId, cancellationToken);
        if (entity is null)
            return Result<AdminCallPolicyDto>.Fail("Arama politikası bulunamadı.", statusCode: 404);

        entity.BitrateLimitEnabled = dto.BitrateLimitEnabled;
        entity.MaxVideoBitrateKbps = Clamp(dto.MaxVideoBitrateKbps, 100, 8000);
        entity.MaxWidth = Clamp(dto.MaxWidth, 160, 1920);
        entity.MaxHeight = Clamp(dto.MaxHeight, 120, 1080);
        entity.MaxFps = Clamp(dto.MaxFps, 10, 60);
        entity.UpdatedBy = updatedBy;

        await _unitOfWork.CallPolicies.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AdminCallPolicyDto>.Ok(entity.ToAdminDto());
    }

    private static int Clamp(int value, int min, int max) => value < min ? min : (value > max ? max : value);

    private static VideoPolicyDto DefaultVideoPolicy() => new()
    {
        Enabled = CallDefinitions.PolicyDefaults.BitrateLimitEnabled,
        MaxBitrateKbps = CallDefinitions.PolicyDefaults.MaxVideoBitrateKbps,
        MaxWidth = CallDefinitions.PolicyDefaults.MaxWidth,
        MaxHeight = CallDefinitions.PolicyDefaults.MaxHeight,
        MaxFps = CallDefinitions.PolicyDefaults.MaxFps
    };
}
