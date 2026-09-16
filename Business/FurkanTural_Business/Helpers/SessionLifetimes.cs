using FurkanTural_Domain.Constants;

namespace FurkanTural_Business.Helpers;

/// <summary>Bir oturumun, kullanıcı ne kadar etkin olursa olsun aşamayacağı mutlak ömür. Süre ilk girişin anından (<c>auth_time</c>) sayılır ve yenilemeyle sıfırlanmaz; yenileme yalnızca bu pencerenin içinde yeni bir jeton verir.<para>Chatural yedi gün, panel ve tanınmayan her kaynak bir saattir. Varsayılan kısa olandır: kaynağı bilinmeyen bir jetonun uzun yaşaması, çalınmış bir jetonun uzun yaşaması demektir.</para></summary>
public static class SessionLifetimes
{
    public static readonly TimeSpan Chat = TimeSpan.FromDays(7);

    public static readonly TimeSpan Default = TimeSpan.FromHours(1);

    public static TimeSpan For(string? appSource)
        => string.Equals(appSource, AppSourceDefinitions.Chat, StringComparison.Ordinal) ? Chat : Default;
}
