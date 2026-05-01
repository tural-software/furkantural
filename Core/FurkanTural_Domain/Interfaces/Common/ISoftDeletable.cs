namespace FurkanTural_Domain.Interfaces.Common;

public interface ISoftDeletable
{
    bool IsActive { get; set; }
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
}