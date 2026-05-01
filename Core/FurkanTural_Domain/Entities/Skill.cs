using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Domain.Entities;

public class Skill : BaseEntity
{
    public string? Name { get; set; }
    public int Proficiency { get; set; }
}