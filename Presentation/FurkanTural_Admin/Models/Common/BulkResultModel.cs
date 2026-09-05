namespace FurkanTural_Admin.Models.Common;

public sealed class BulkResultModel
{
    public int Requested { get; set; }
    public int Affected { get; set; }
    public List<int> Skipped { get; set; } = [];
}
