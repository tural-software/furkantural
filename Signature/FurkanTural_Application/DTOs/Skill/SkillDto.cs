namespace FurkanTural_Application.DTOs.Skill;

/// <summary>Portfolyodaki yetenek kaydı. Proficiency bir seviye kademesi değil yüzde değeridir; 0-100 aralığında doğrulanır, dışına çıkan istek reddedilir.</summary>
public class SkillDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public int Proficiency { get; set; }
}
