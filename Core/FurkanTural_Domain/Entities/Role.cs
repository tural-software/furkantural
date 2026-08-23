using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Domain.Entities;

/// <summary><see cref="User"/> rolü. Tohumlanan kayıtlar: Admin, User, Subscriber, Visitor.</summary>
public class Role : BaseEntity
{
    public string? Name { get; set; }
}
