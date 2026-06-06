using FurkanTural_Application.DTOs.Call;
using FurkanTural_Domain.Entities;

namespace FurkanTural_Business.Mappers;

public static class CallPolicyMapper
{
    public static AdminCallPolicyDto ToAdminDto(this CallPolicy e) => new()
    {
        Id = e.Id,
        BitrateLimitEnabled = e.BitrateLimitEnabled,
        MaxVideoBitrateKbps = e.MaxVideoBitrateKbps,
        MaxWidth = e.MaxWidth,
        MaxHeight = e.MaxHeight,
        MaxFps = e.MaxFps,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt,
        UpdatedBy = e.UpdatedBy
    };

    public static VideoPolicyDto ToVideoPolicyDto(this CallPolicy e) => new()
    {
        Enabled = e.BitrateLimitEnabled,
        MaxBitrateKbps = e.MaxVideoBitrateKbps,
        MaxWidth = e.MaxWidth,
        MaxHeight = e.MaxHeight,
        MaxFps = e.MaxFps
    };
}
