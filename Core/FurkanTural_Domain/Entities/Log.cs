using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Domain.Entities;

public class Log : BaseEntity
{
    public string? Project { get; set; }
    public DateTime Date { get; set; }
    public string? Level { get; set; }
    public string? Message { get; set; }
    public string? Detail { get; set; }
    public string? IpAddress { get; set; }
    public string? Path { get; set; }
}