using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Domain.Entities;

public class User : BaseEntity
{
    public string? Username { get; set; }
    public string? Password { get; set; }
    public int RoleId { get; set; }

    // Chat / üyelik alanları
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }

    // Aktiflik: kullanıcının en son çevrimdışı olduğu an ("son görülme").
    public DateTime? LastSeenAt { get; set; }

    // Üyelik sözleşmesi onayı (KVKK / hizmet şartları): kabul anı ve kabul edilen sürüm.
    public DateTime? MembershipAgreementAcceptedAt { get; set; }
    public string? MembershipAgreementVersion { get; set; }
}
