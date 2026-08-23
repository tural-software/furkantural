using Asp.Versioning;
using FurkanTural_Application.Wrappers;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_API.Controllers.Base;

/// <summary>Her uçtan dönen <see cref="Result"/> zarfını gövdeye yazar ve zarfın StatusCode'unu HTTP durum koduna çevirir; istemcinin gördüğü kod ile gövdedeki kod bu yüzden hep aynıdır.<para>Başarısız bir zarf InternalMessage taşıyorsa o metin buradan günlüğe düşer. Alan <c>JsonIgnore</c> olduğu için yanıta hiç çıkmaz, dolayısıyla tek okunma yeri burasıdır: kaydedilmezse yazılmış olması hiçbir işe yaramaz. Seviye Information'dır — başarısızlığın kendisi zaten durum kodundan görünür, buradaki metin ona eklenen teşhistir ve Warning'e çıkarmak gerçek uyarıları gürültüye boğardı.</para></summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    protected IActionResult ToActionResult(Result result)
    {
        WriteInternalMessage(result);
        return StatusCode(result.StatusCode, result);
    }

    protected IActionResult ToActionResult<T>(Result<T> result)
    {
        WriteInternalMessage(result);
        return StatusCode(result.StatusCode, result);
    }

    protected IActionResult ToActionResult<T>(PagedResult<T> result)
    {
        WriteInternalMessage(result);
        return StatusCode(result.StatusCode, result);
    }

    private void WriteInternalMessage(Result result)
    {
        if (result.Success || string.IsNullOrWhiteSpace(result.InternalMessage)) return;

        var context = ControllerContext.HttpContext;
        if (context is null) return;

        context.RequestServices?.GetService<ILoggerFactory>()
            ?.CreateLogger(GetType())
            .LogInformation("{Method} {Path} -> {StatusCode}: {InternalMessage}",
                context.Request.Method, context.Request.Path, result.StatusCode, result.InternalMessage);
    }
}
