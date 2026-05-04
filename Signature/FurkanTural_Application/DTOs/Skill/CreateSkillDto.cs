namespace FurkanTural_Application.DTOs.Skill;

public class CreateSkillDto
{
    public string? Name { get; set; }
    public int Proficiency { get; set; }
    public int? CreatedBy { get; set; }
}