namespace FurkanTural_Domain.Abstract;

public interface IInsertable
{
    DateTime CreatedAt { get; set; }
    int? CreatedBy { get; set; }
}