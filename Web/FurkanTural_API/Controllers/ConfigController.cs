using Asp.Versioning;
using FurkanTural_API.Controllers.Base;
using FurkanTural_Application.Wrappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_API.Controllers;

/// <summary>
/// Kayıtlı ön-yüz uygulamalarına (app-token sahibi) yalnızca izin verilen
/// config değerlerini (startup'ta çözülmüş halde) sunar. Şifre çözme mantığı
/// ön-yüzlere taşınmaz; her app yalnızca kendisine allowlist'lenen anahtarları görür.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = "AppClient")]
public class ConfigController(IConfiguration configuration) : BaseApiController
{
    private readonly IConfiguration _configuration = configuration;

    /// <summary>
    /// Çağıran uygulamanın (app_source) izinli config anahtarlarını çözülmüş halde döndürür.
    /// </summary>
    [HttpGet("app")]
    public IActionResult GetAppConfig()
    {
        var appSource = User.FindFirst("app_source")?.Value;
        if (string.IsNullOrWhiteSpace(appSource))
            return ToActionResult(Result<Dictionary<string, string?>>.Fail("Geçersiz uygulama kaynağı.", statusCode: 403));

        var allowedKeys = _configuration.GetSection($"AppConfigAccess:{appSource}").Get<string[]>() ?? [];

        var result = new Dictionary<string, string?>();
        foreach (var key in allowedKeys)
            result[key] = _configuration[key];

        return ToActionResult(Result<Dictionary<string, string?>>.Ok(result));
    }
}