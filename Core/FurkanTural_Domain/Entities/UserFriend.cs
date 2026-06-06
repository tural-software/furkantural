using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Domain.Entities;

/// <summary>
/// Kullanıcılar arası arkadaşlık ilişkisi. Tek satır + <see cref="StatusId"/> ile
/// durum (Pending/Accepted/Rejected/Blocked) genel <see cref="Status"/> tablosuna bağlanır.
/// </summary>
public class UserFriend : BaseEntity
{
    public int RequesterId { get; set; }
    public int AddresseeId { get; set; }
    public int StatusId { get; set; }
    public DateTime? RespondedAt { get; set; }

    /// <summary>Durum "Blocked" ise engelleme işlemini başlatan kullanıcı (yalnızca o engeli kaldırabilir).</summary>
    public int? BlockedByUserId { get; set; }
}
