namespace FurkanTural_Application.DTOs.Education;

public class UpdateEducationDto
{
    public int Id { get; set; }
    public string? Institution { get; set; }
    public string? Degree { get; set; }
    public string? FieldOfStudy { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}