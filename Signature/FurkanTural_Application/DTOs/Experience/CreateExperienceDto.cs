namespace FurkanTural_Application.DTOs.Experience;

public class CreateExperienceDto
{
    public string? Position { get; set; }
    public string? CompanyName { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? CreatedBy { get; set; }
}