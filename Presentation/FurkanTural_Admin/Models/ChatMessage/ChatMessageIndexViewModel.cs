namespace FurkanTural_Admin.Models.ChatMessage;

public sealed class ChatMessageIndexViewModel
{
    public IReadOnlyList<ChatMessageAdminDto> Rows { get; init; } = [];
    public int TotalCount { get; init; }
    public int ActiveCount { get; init; }
    public int PassiveCount { get; init; }
    public int DeletedCount { get; init; }

    public string? SearchContent { get; init; }
    public string? UsernameFilter { get; init; }
    public string? TypeFilter { get; init; }
    public string? ActiveFilter { get; init; }
    public string? DeletedFilter { get; init; }
    public string? DateFrom { get; init; }
    public string? DateTo { get; init; }

    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public int TotalFiltered { get; init; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalFiltered / PageSize) : 0;
}
