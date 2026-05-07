namespace FurkanTural_API.Models.Experience;

public class CreateExperienceRequest
{
    public string? Position { get; set; }
    public string? CompanyName { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class UpdateExperienceRequest
{
    public int Id { get; set; }
    public string? Position { get; set; }
    public string? CompanyName { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
