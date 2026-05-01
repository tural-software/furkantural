namespace FurkanTural_Domain.Interfaces.Common;

public interface IInsertable
{
    DateTime CreatedAt { get; set; }
    int? CreatedBy { get; set; }
}