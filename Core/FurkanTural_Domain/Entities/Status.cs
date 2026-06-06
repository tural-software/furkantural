using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Domain.Entities;

/// <summary>
/// Site genelinde yeniden kullanılabilir durum (lookup) tablosu.
/// Her kullanım kendi <see cref="Group"/> adıyla filtreler (örn. "Friendship").
/// </summary>
public class Status : BaseEntity
{
    public string? Group { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Color { get; set; }
    public int SortOrder { get; set; }
}
