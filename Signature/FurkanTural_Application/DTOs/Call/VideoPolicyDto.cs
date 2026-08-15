namespace FurkanTural_Application.DTOs.Call;

/// <summary>
/// Arama politikasının istemciye verilen hâli. Politika tablosunda tek küresel satır (Id = 1) vardır,
/// kullanıcıya göre dallanma henüz yoktur; satır silinmiş ya da pasifse
/// <see cref="FurkanTural_Domain.Constants.CallDefinitions.PolicyDefaults"/> değerleri döner, yani
/// istemci hiçbir koşulda politikasız kalmaz. Alan adları yönetim tarafındaki karşılığıyla birebir
/// değildir: Enabled ile BitrateLimitEnabled, MaxBitrateKbps ile MaxVideoBitrateKbps aynı şeyi anlatır.
/// </summary>
public class VideoPolicyDto
{
    public bool Enabled { get; set; }
    public int MaxBitrateKbps { get; set; }
    public int MaxWidth { get; set; }
    public int MaxHeight { get; set; }
    public int MaxFps { get; set; }
}