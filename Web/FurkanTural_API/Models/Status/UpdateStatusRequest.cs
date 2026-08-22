namespace FurkanTural_API.Models.Status;

public class UpdateStatusRequest
{
    public int Id { get; set; }
    public string? Group { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Color { get; set; }
    public int SortOrder { get; set; }
}