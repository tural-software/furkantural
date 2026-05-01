using FurkanTural_Domain.Interfaces.Common;

namespace FurkanTural_Domain.Entities.Common;

public abstract class BaseEntity : IAuditable, ISoftDeletable
{
    public int Id { get; set; }

    // IInsertable (via IAuditable)
    public DateTime CreatedAt { get; set; }
    public int? CreatedBy { get; set; }

    // IAuditable
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }

    // ISoftDeletable
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
}