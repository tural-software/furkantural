namespace FurkanTural_Admin.Models.Wrappers;

public class ApiResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int StatusCode { get; set; } = 200;
    public List<string> Errors { get; set; } = new();

    public static ApiResult Fail(string error, int statusCode = 500) =>
        new() { Success = false, Errors = new List<string> { error }, StatusCode = statusCode };
}

public class ApiResult<T> : ApiResult
{
    public T? Data { get; set; }

    public static new ApiResult<T> Fail(string error, int statusCode = 500) =>
        new() { Success = false, Errors = new List<string> { error }, StatusCode = statusCode };
}
