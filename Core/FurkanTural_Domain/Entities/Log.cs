using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Domain.Entities;

/// <summary>Uygulama olay kaydı. Project ve Level'ın sabit listesi yoktur, serbest metindir; API tarafında ActivityLogger bunları "FurkanTural_API" ve "Information" olarak yazar.</summary>
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
