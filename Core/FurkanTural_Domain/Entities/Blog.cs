using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Domain.Entities;

public class Blog : BaseEntity
{
    public string? Title { get; set; }
    public string? Content { get; set; }
}