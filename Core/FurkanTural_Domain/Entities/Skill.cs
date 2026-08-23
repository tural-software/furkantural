using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Domain.Entities;

/// <summary>Portfolyodaki yetenek kaydı; Proficiency yüzde değeridir ve serviste 0-100 aralığında doğrulanır.</summary>
public class Skill : BaseEntity
{
    public string? Name { get; set; }
    public int Proficiency { get; set; }
}
