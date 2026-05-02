namespace FurkanTural_API.Models.Skill;

public class CreateSkillRequest
{
    public string? Name { get; set; }
    public int Proficiency { get; set; }
}

public class UpdateSkillRequest
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public int Proficiency { get; set; }
}
