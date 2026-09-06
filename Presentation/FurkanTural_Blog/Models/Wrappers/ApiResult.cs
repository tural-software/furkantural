namespace FurkanTural_Blog.Models.Wrappers;

public class ApiResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int StatusCode { get; set; } = 200;

    /// <summary>Başarısız yanıtta okunacak metin buradadır: zarf Ok yolunda Message'ı, Fail yolunda Errors'ı doldurur ve ikisi birlikte dolmaz.</summary>
    public List<string> Errors { get; set; } = [];
}

public class ApiResult<T> : ApiResult
{
    public T? Data { get; set; }
}