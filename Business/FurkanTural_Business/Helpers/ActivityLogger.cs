using FurkanTural_Application.DTOs.Log;
using FurkanTural_Application.Services.Abstract;
using Microsoft.AspNetCore.Http;

namespace FurkanTural_Business.Helpers;

public sealed class ActivityLogger(ILogService logService, IHttpContextAccessor httpContextAccessor)
{
    public async Task LogAsync(string message, CancellationToken cancellationToken = default)
    {
        try
        {
            await logService.CreateAsync(new CreateLogDto
            {
                Project = "FurkanTural_API",
                Date = DateTime.UtcNow,
                Level = "Information",
                Message = message,
                IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                Path = httpContextAccessor.HttpContext?.Request.Path
            }, cancellationToken);
        }
        catch { }
    }
}
