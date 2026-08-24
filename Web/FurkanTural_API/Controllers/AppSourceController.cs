using Asp.Versioning;
using FurkanTural_API.Controllers.Base;
using FurkanTural_Application.Services.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_API.Controllers;

[ApiVersion("1.0")]
public class AppSourceController(IAppSourceService appSourceService) : JwtBaseController
{
    private readonly IAppSourceService _appSourceService = appSourceService;

    /// <summary>Etkin sunum projelerini listele (posta şablonu ataması için)</summary>
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        => ToActionResult(await _appSourceService.GetAllAsync(cancellationToken));
}
