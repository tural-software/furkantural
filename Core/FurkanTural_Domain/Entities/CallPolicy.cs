using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Domain.Entities;

/// <summary>
/// Görüntülü arama kalite politikası — tek küresel satır (Id=1), başlangıç değerleri
/// <see cref="Constants.CallDefinitions.PolicyDefaults"/> ile tohumlanır.
/// </summary>
public class CallPolicy : BaseEntity
{
    public bool BitrateLimitEnabled { get; set; }
    public int MaxVideoBitrateKbps { get; set; }
    public int MaxWidth { get; set; }
    public int MaxHeight { get; set; }
    public int MaxFps { get; set; }
}
