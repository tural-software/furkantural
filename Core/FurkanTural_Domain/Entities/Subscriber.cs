using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Domain.Entities;

/// <summary>
/// Uygulama bülteni abonesi.
/// </summary>
public class Subscriber : BaseEntity
{
    public string? Email { get; set; }
}