namespace FurkanTural_Domain.Abstract;

public interface ISoftDeletable
{
    bool IsActive { get; set; }
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
    int? DeletedBy { get; set; }
}