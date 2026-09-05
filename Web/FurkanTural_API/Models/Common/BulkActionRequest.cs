namespace FurkanTural_API.Models.Common;

public class BulkActionRequest
{
    public List<int>? Ids { get; set; }
    public string? Action { get; set; }
}
