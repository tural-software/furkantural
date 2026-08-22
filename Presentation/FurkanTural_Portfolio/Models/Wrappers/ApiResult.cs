namespace FurkanTural_Portfolio.Models.Wrappers;

public class ApiResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int StatusCode { get; set; } = 200;
}

public class ApiResult<T> : ApiResult
{
    public T? Data { get; set; }
}