namespace FurkanTural_Blog.Models.Wrappers;

/// <summary>API'nin <c>PagedResult&lt;T&gt;</c> gövdesini karşılayan istemci modeli (data + sayfalama meta alanları aynı düzeyde döner).</summary>
public class PagedApiResult<T> : ApiResult
{
    public T? Data { get; set; }
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}
