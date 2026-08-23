using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Domain.Entities;

/// <summary>Uygulama kullanıcısı; RoleId <see cref="Role"/>'e bakar. Password düz metin değil PBKDF2 türetimidir. MembershipAgreementVersion kabul anındaki <see cref="Constants.AgreementDefinitions.CurrentVersion"/> değerini saklar; sabit ilerletilirse eşleşme bozulur ve mevcut üyeden yeniden onay istenir. LastSeenAt son çevrimdışı olunan andır.</summary>
public class User : BaseEntity
{
    public string? Username { get; set; }
    public string? Password { get; set; }
    public int RoleId { get; set; }

    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }

    public DateTime? LastSeenAt { get; set; }

    public DateTime? MembershipAgreementAcceptedAt { get; set; }
    public string? MembershipAgreementVersion { get; set; }
}
