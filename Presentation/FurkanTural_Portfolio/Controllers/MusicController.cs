using Microsoft.AspNetCore.Mvc;
using FurkanTural_Portfolio.Models;
using FurkanTural_Portfolio.Services;

namespace FurkanTural_Portfolio.Controllers;

public class MusicController(IPortfolioApiService apiService, IConfiguration configuration) : Controller
{
    private readonly IPortfolioApiService _apiService = apiService;
    private readonly IConfiguration _configuration = configuration;

    public async Task<IActionResult> Detail(int id, CancellationToken ct)
    {
        if (id <= 0)
            return RedirectToAction("Index", "Home");

        var song = await _apiService.GetSongByIdAsync(id, ct);
        if (song is null)
            return NotFound();   // markalı 404 (UseStatusCodePagesWithReExecute)

        var images = await _apiService.GetSongImagesAsync(id, ct);
        var apiBase = (_configuration["Api:BaseUrl"] ?? string.Empty).TrimEnd('/');

        ViewData["Title"] = song.Name;
        ViewData["MetaDescription"] = string.Join(" • ", new[] { song.Artist, song.Album }
            .Where(s => !string.IsNullOrWhiteSpace(s)));

        var cover = images.FirstOrDefault(i => i.IsCover) ?? images.FirstOrDefault();
        if (cover?.Url is { Length: > 0 } coverUrl)
            ViewData["OgImage"] = $"{apiBase}/{coverUrl}";

        return View(new SongDetailViewModel
        {
            Song = song,
            Images = images,
            ApiBaseUrl = apiBase
        });
    }
}