namespace FurkanTural_Domain.Interfaces.Common;

public interface IAuditable : IInsertable
{
    DateTime? UpdatedAt { get; set; }
    int? UpdatedBy { get; set; }
}