using Asp.Versioning;
using FurkanTural_API.Controllers.Base;
using FurkanTural_Application.Services.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_API.Controllers;

[ApiVersion("1.0")]
[Authorize(Policy = "AdminOnly")]
public class DataRetentionController(IDataRetentionService dataRetentionService) : JwtBaseController
{
    private readonly IDataRetentionService _dataRetentionService = dataRetentionService;

    /// <summary>Saklama süresi dolmuş verinin kategori bazında sayısı (silmez)</summary>
    [HttpGet("admin/preview")]
    public async Task<IActionResult> Preview(CancellationToken cancellationToken)
        => ToActionResult(await _dataRetentionService.PreviewAsync(cancellationToken));

    /// <summary>Saklama süresi dolmuş veriyi ve dosyalarını kalıcı olarak siler</summary>
    [HttpPost("admin/purge")]
    public async Task<IActionResult> Purge(CancellationToken cancellationToken)
        => ToActionResult(await _dataRetentionService.PurgeAsync(SortUserId(), cancellationToken));
}
