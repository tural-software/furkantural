using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using FurkanTural_Portfolio.Models;
using FurkanTural_Portfolio.Services;

namespace FurkanTural_Portfolio.Controllers;

public class HomeController(IPortfolioApiService apiService, IPortfolioContactClient contactClient, IAppConfigService appConfigService) : Controller
{
    private readonly IPortfolioApiService _apiService = apiService;
    private readonly IPortfolioContactClient _contactClient = contactClient;
    private readonly IAppConfigService _appConfigService = appConfigService;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        // Bağımsız okumalar; sıralı beklemek yerine paralel çalıştırılır.
        var skillsTask = _apiService.GetSkillsAsync(ct);
        var projectsTask = _apiService.GetProjectsAsync(ct);
        var songsTask = _apiService.GetSongsAsync(ct);
        var experiencesTask = _apiService.GetExperiencesAsync(ct);
        var educationsTask = _apiService.GetEducationsAsync(ct);

        await Task.WhenAll(skillsTask, projectsTask, songsTask, experiencesTask, educationsTask);

        var vm = new IndexViewModel
        {
            Skills = await skillsTask,
            Projects = await projectsTask,
            Songs = await songsTask,
            Experiences = await experiencesTask,
            Educations = await educationsTask
        };

        ViewBag.TurnstileSiteKey = await _appConfigService.GetTurnstileSiteKeyAsync(ct);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact([FromForm] ContactFormModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { message = "Lütfen tüm alanları doldurun." });

        // MVC-katmanı savunması: Turnstile token boş veya eksikse formu reddet.
        // (API katmanı zaten Turnstile'ı doğrular; bu erken-ret spam isteklerini azaltır.)
        if (string.IsNullOrWhiteSpace(model.TurnstileToken))
            return BadRequest(new { message = "Bot koruması doğrulaması eksik. Lütfen tekrar deneyin." });

        var ok = await _contactClient.SubmitContactAsync(model, ct);
        if (ok)
            return Ok(new { message = "Mesajınız başarıyla gönderildi!" });

        return StatusCode(500, new { message = "Mesaj gönderilemedi. Lütfen tekrar deneyin." });
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(int? code = null)
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            StatusCode = code
        });
    }
}
