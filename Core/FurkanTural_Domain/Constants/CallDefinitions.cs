namespace FurkanTural_Domain.Constants;

/// <summary><see cref="Entities.CallLog"/> için tür ve durum sabitleri; <see cref="PolicyDefaults"/> ise tekil <see cref="Entities.CallPolicy"/> satırının tohum değerlerini taşır.</summary>
public static class CallDefinitions
{
    public static class Types
    {
        public const string Audio = "Audio";
        public const string Video = "Video";
    }

    public static class Statuses
    {
        public const string Ringing = "Ringing";
        public const string Answered = "Answered";
        public const string Ended = "Ended";
        public const string Rejected = "Rejected";
        public const string Missed = "Missed";
        public const string Canceled = "Canceled";
        public const string Failed = "Failed";
    }

    public static bool IsValidType(string? type) =>
        string.Equals(type, Types.Audio, System.StringComparison.OrdinalIgnoreCase) ||
        string.Equals(type, Types.Video, System.StringComparison.OrdinalIgnoreCase);

    public static class PolicyDefaults
    {
        public const bool BitrateLimitEnabled = true;
        public const int MaxVideoBitrateKbps = 600;
        public const int MaxWidth = 640;
        public const int MaxHeight = 480;
        public const int MaxFps = 24;
    }
}
