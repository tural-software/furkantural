namespace FurkanTural_Admin.Models.DataRetention;

public class DataRetentionCategoryModel
{
    public string? Key { get; set; }
    public string? Label { get; set; }
    public int Count { get; set; }
}

public class DataRetentionReportModel
{
    public DateTime Cutoff { get; set; }
    public List<DataRetentionCategoryModel> Categories { get; set; } = [];
    public int FilesDeleted { get; set; }
    public int FilesFailed { get; set; }
    public int Total => Categories.Sum(c => c.Count);
}

public class DataRetentionViewModel
{
    public DataRetentionReportModel? Preview { get; set; }
    public DataRetentionReportModel? Purged { get; set; }
    public string? Error { get; set; }
}
