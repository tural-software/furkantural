using Microsoft.AspNetCore.Mvc;
using FurkanTural_Portfolio.Models;
using FurkanTural_Portfolio.Services;

namespace FurkanTural_Portfolio.Controllers;

public class ProjectsController(IPortfolioApiService apiService, IConfiguration configuration) : Controller
{
    private readonly IPortfolioApiService _apiService = apiService;
    private readonly IConfiguration _configuration = configuration;

    public async Task<IActionResult> Detail(int id, CancellationToken ct)
    {
        if (id <= 0)
            return RedirectToAction("Index", "Home");

        var project = await _apiService.GetProjectByIdAsync(id, ct);
        if (project is null)
            return NotFound();   // markalı 404 (UseStatusCodePagesWithReExecute)

        var images = await _apiService.GetProjectImagesAsync(id, ct);
        var apiBase = (_configuration["Api:BaseUrl"] ?? string.Empty).TrimEnd('/');

        ViewData["Title"] = project.Title;
        ViewData["MetaDescription"] = project.ShortDescription;

        var cover = images.FirstOrDefault(i => i.IsCover) ?? images.FirstOrDefault();
        if (cover?.Url is { Length: > 0 } coverUrl)
            ViewData["OgImage"] = $"{apiBase}/{coverUrl}";

        return View(new ProjectDetailViewModel
        {
            Project = project,
            Images = images,
            ApiBaseUrl = apiBase
        });
    }
}
