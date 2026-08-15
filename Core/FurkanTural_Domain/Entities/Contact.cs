using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Domain.Entities;

/// <summary>
/// Ziyaretçinin iletişim formundan bıraktığı mesaj; ardından gönderilen e-postaların gövdesi
/// <see cref="ContactTemplate"/>'ten okunur.
/// </summary>
public class Contact : BaseEntity
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Message { get; set; }
    public bool IsRead { get; set; } = false;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
