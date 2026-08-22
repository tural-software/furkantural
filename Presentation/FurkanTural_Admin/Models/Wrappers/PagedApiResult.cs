namespace FurkanTural_Admin.Models.Wrappers;

public class PagedApiResult<T> : ApiResult<IEnumerable<T>>
{
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }
}