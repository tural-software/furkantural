using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Domain.Entities;

public class Experience : BaseEntity
{
    public string? Position { get; set; }
    public string? CompanyName { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
