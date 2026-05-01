namespace FurkanTural_Domain.Abstract;

public interface IAuditable : IInsertable
{
    DateTime? UpdatedAt { get; set; }
    int? UpdatedBy { get; set; }
}