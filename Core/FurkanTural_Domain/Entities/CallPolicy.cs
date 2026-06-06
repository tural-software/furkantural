using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Domain.Entities;

/// <summary>
/// Görüntülü arama kalite/bant genişliği politikası (tek küresel satır, Id=1).
/// İstemci her arama başında efektif politikayı çeker ve uygular.
/// </summary>
public class CallPolicy : BaseEntity
{
    /// <summary>Video bit hızı sınırı uygulansın mı?</summary>
    public bool BitrateLimitEnabled { get; set; }
    public int MaxVideoBitrateKbps { get; set; }
    public int MaxWidth { get; set; }
    public int MaxHeight { get; set; }
    public int MaxFps { get; set; }
}
