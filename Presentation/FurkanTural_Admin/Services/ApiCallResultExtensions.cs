using System.Text.Json;
using FurkanTural_Admin.Models.Common;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_Admin.Services;

internal static class ApiCallResultExtensions
{
    /// <summary>
    /// HttpResponse → ApiCallResult. Başarısızsa API'nin Result zarfından mesajı (errors[0] veya
    /// message) çıkarır; gövde JSON değilse durum kodu yine de korunur.
    /// </summary>
    public static async Task<ApiCallResult> ToApiCallResultAsync(this HttpResponseMessage response, CancellationToken ct = default)
    {
        if (response.IsSuccessStatusCode)
            return ApiCallResult.Ok();

        string? message = null;
        try
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!string.IsNullOrWhiteSpace(body))
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                if (root.TryGetProperty("errors", out var errors)
                    && errors.ValueKind == JsonValueKind.Array && errors.GetArrayLength() > 0)
                    message = errors[0].GetString();
                else if (root.TryGetProperty("message", out var msg) && msg.ValueKind == JsonValueKind.String)
                    message = msg.GetString();
            }
        }
        catch
        {
            // Gövde JSON değilse (örneğin boş bir 401) durum kodu tek başına yeterli bilgidir.
        }

        return ApiCallResult.Fail((int)response.StatusCode, message);
    }

    /// <summary>
    /// ApiCallResult → IActionResult. 401 → Unauthorized (form-modal.js login'e yönlendirir),
    /// diğer hatalar → gerçek durum kodu + mesaj (yoksa fallback). Generic 500 maskelemesini bitirir.
    /// </summary>
    public static IActionResult ToActionResult(this ApiCallResult result, string fallbackMessage)
    {
        if (result.Success)
            return new OkResult();
        if (result.StatusCode == 401)
            return new UnauthorizedResult();

        var code = result.StatusCode is >= 400 and < 600 ? result.StatusCode : 500;
        return new ObjectResult(new { message = result.Message ?? fallbackMessage }) { StatusCode = code };
    }
}