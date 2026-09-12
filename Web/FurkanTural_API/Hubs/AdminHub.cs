using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FurkanTural_API.Hubs;

[Authorize(Policy = "AdminOnly")]
public class AdminHub : Hub
{
}
