using FurkanTural_Application.Services.Abstract;
using FurkanTural_API.Controllers.Base;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace FurkanTural_API.Controllers;

[Authorize(Policy = "AdminOnly")]
[ApiVersion("1.0")]
public class DashboardController(IAdminDashboardService dashboardService) : JwtBaseController
{
    private readonly IAdminDashboardService _dashboardService = dashboardService;

    /// <summary>Yönetim panelinin açılış verisi tek yanıtta: yirmi bir varlığın özeti, okunmamış iletişim ve bekleyen şikayet sayısı, pencere içinde görülen aktif kullanıcı ve iki haftalık sayaç. windowDays 1-90 gün (varsayılan 7); today verilmezse sunucunun UTC günü</summary>
    [HttpGet("admin/summary")]
    public async Task<IActionResult> GetAdminSummary([FromQuery] DateTime? today, [FromQuery] int windowDays = 7, CancellationToken cancellationToken = default)
        => ToActionResult(await _dashboardService.GetAsync(today?.Date ?? DateTime.UtcNow.Date, windowDays, cancellationToken));
}
