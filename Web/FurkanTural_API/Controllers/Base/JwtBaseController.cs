using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

namespace FurkanTural_API.Controllers.Base;

public abstract class JwtBaseController : BaseApiController
{
    protected int? SortUserId()
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return int.TryParse(sub, out var id) ? id : null;
    }

    protected string? SortUserRole()
        => User.FindFirst(ClaimTypes.Role)?.Value;
}