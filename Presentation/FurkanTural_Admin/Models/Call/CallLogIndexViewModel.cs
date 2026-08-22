namespace FurkanTural_Admin.Models.Call;

public sealed class CallLogIndexViewModel
{
    /// <summary>Arama (video bit hızı) politikası — yalnızca Index'te ayar kartı için doldurulur.</summary>
    public CallPolicyFormDto? Policy { get; set; }

    public IReadOnlyList<CallLogAdminDto> Rows { get; init; } = [];
    public int TotalCount { get; init; }
    public int ActiveCount { get; init; }
    public int PassiveCount { get; init; }
    public int DeletedCount { get; init; }

    public string? Search { get; init; }
    public string? TypeFilter { get; init; }
    public string? StatusFilter { get; init; }
    public string? ActiveFilter { get; init; }
    public string? DeletedFilter { get; init; }
    public string? DateFrom { get; init; }
    public string? DateTo { get; init; }

    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public int TotalFiltered { get; init; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalFiltered / PageSize) : 0;
}