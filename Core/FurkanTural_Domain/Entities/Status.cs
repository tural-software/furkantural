using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Domain.Entities;

/// <summary>
/// Farklı akışların paylaştığı durum sözlüğü. Bir satır Group + Code ikilisiyle adreslenir ve
/// servisler Id yerine <see cref="Constants.StatusDefinitions"/> sabitleriyle çözer.
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
