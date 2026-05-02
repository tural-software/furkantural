using Asp.Versioning;
using FurkanTural_Application.Wrappers;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_API.Controllers.Base;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    protected IActionResult ToActionResult(Result result)
        => StatusCode(result.StatusCode, result);

    protected IActionResult ToActionResult<T>(Result<T> result)
        => StatusCode(result.StatusCode, result);

    protected IActionResult ToActionResult<T>(PagedResult<T> result)
        => StatusCode(result.StatusCode, result);
}