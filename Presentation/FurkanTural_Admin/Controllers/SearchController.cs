using FurkanTural_Admin.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FurkanTural_Admin.Controllers;

public class SearchController(IAdminSearch search) : Controller
{
    private readonly IAdminSearch _search = search;

    [HttpGet]
    public async Task<IActionResult> Index(string? q, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var groups = await _search.SearchAsync(q, token, cancellationToken);
        return Json(new
        {
            query = (q ?? "").Trim(),
            groups = groups.Select(g => new
            {
                slug = g.Slug,
                title = g.Title,
                items = g.Hits.Select(h => new
                {
                    id = h.Id,
                    label = h.Label,
                    url = Url.Action("Index", g.Controller, new RouteValueDictionary { [h.RouteKey] = h.RouteValue })
                })
            })
        });
    }
}
