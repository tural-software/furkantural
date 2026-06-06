namespace FurkanTural_Domain.Constants;

/// <summary>
/// <see cref="Entities.CallLog"/> için tür ve durum sabitleri.
/// </summary>
public static class CallDefinitions
{
    public static class Types
    {
        public const string Audio = "Audio";
        public const string Video = "Video";
    }

    public static class Statuses
    {
        public const string Ringing = "Ringing";   // çalıyor (cevap bekleniyor)
        public const string Answered = "Answered";  // cevaplandı (görüşme sürüyor)
        public const string Ended = "Ended";        // normal sonlandı
        public const string Rejected = "Rejected";  // alıcı reddetti
        public const string Missed = "Missed";      // cevaplanmadı (arayan vazgeçti / zaman aşımı)
        public const string Canceled = "Canceled";  // arayan iptal etti
        public const string Failed = "Failed";      // teknik hata
    }

    public static bool IsValidType(string? type) =>
        string.Equals(type, Types.Audio, System.StringComparison.OrdinalIgnoreCase) ||
        string.Equals(type, Types.Video, System.StringComparison.OrdinalIgnoreCase);

    /// <summary>Görüntülü arama bit hızı politikasının varsayılanları (seed + fallback).</summary>
    public static class PolicyDefaults
    {
        public const bool BitrateLimitEnabled = true;
        public const int MaxVideoBitrateKbps = 600;
        public const int MaxWidth = 640;
        public const int MaxHeight = 480;
        public const int MaxFps = 24;
    }
}
