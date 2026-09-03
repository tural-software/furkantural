namespace FurkanTural_Application.DTOs.Skill;

public class AdminSkillDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public int Proficiency { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
    public int? DeletedBy { get; set; }
}