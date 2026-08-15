using System.Text.Json.Serialization;

namespace FurkanTural_Application.Wrappers;

/// <summary>
/// API yanıt zarfı; BaseApiController zarfı gövdeye yazıp StatusCode'u HTTP durum koduna çevirir.
/// Message ile Errors birlikte dolmaz: Ok yalnızca Message'ı, Fail yalnızca Errors'ı doldurur, bu
/// yüzden başarısız yanıtta okunacak metin Errors[0]'dadır. InternalMessage geliştirici teşhisidir
/// ve JsonIgnore ile süreç içinde kalır; istemciye çıkan tek hata metni Errors'tır.
/// </summary>
public class Result
{
    public bool Success { get; protected init; }
    public bool IsFailure => !Success;
    public string Message { get; protected init; } = string.Empty;

    [JsonIgnore]
    public string InternalMessage { get; protected init; } = string.Empty;

    public int StatusCode { get; protected init; } = 200;
    public List<string> Errors { get; protected init; } = [];

    protected Result() { }

    public static Result Ok(string message = "") =>
        new() { Success = true, Message = message, StatusCode = 200 };

    public static Result Fail(string error, string internalMessage = "", int statusCode = 400) =>
        new() { Success = false, Errors = [error], InternalMessage = internalMessage, StatusCode = statusCode };

    public static Result Fail(List<string> errors, string internalMessage = "", int statusCode = 400) =>
        new() { Success = false, Errors = errors, InternalMessage = internalMessage, StatusCode = statusCode };
}

/// <summary>
/// Veri taşıyan <see cref="Result"/>; Fail yollarında Data null kalır.
/// </summary>
public class Result<T> : Result
{
    public T? Data { get; protected init; }

    protected Result() { }

    public static Result<T> Ok(T data, string message = "") =>
        new() { Success = true, Data = data, Message = message, StatusCode = 200 };

    public new static Result<T> Fail(string error, string internalMessage = "", int statusCode = 400) =>
        new() { Success = false, Errors = [error], InternalMessage = internalMessage, StatusCode = statusCode };

    public new static Result<T> Fail(List<string> errors, string internalMessage = "", int statusCode = 400) =>
        new() { Success = false, Errors = errors, InternalMessage = internalMessage, StatusCode = statusCode };
}

/// <summary>
/// Sayfalama bilgisiyle liste döndüren <see cref="Result{T}"/>. TotalPages, HasPreviousPage ve
/// HasNextPage saklanmaz; TotalCount, PageNumber ve PageSize üçlüsünden hesaplanır. Sayfalı yanıtta
/// bu sınıfın beş parametreli Ok'u çağrılmalıdır: taban sınıftan devralınan iki parametreli Ok
/// PagedResult değil düz bir <see cref="Result{T}"/> döndürür ve sayfalama alanları kaybolur.
/// </summary>
public class PagedResult<T> : Result<IEnumerable<T>>
{
    public int TotalCount { get; private init; }
    public int PageNumber { get; private init; }
    public int PageSize { get; private init; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    private PagedResult() { }

    public static PagedResult<T> Ok(IEnumerable<T> data, int totalCount, int pageNumber, int pageSize, string message = "") =>
        new() { Success = true, Data = data, Message = message, StatusCode = 200, TotalCount = totalCount, PageNumber = pageNumber, PageSize = pageSize };

    public new static PagedResult<T> Fail(string error, string internalMessage = "", int statusCode = 400) =>
        new() { Success = false, Errors = [error], InternalMessage = internalMessage, StatusCode = statusCode };
}