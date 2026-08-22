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

    /// <summary>
    /// Anasayfanın beş bölümü birbirinden bağımsız uçlardan beslenir ve hepsi aynı anda başlatılır.
    /// Sırayla beklenseydi sayfanın açılma süresi beş çağrının toplamı olurdu; böyle en yavaş
    /// olanın süresi kadardır.
    /// </summary>
    public async Task<IActionResult> Index(CancellationToken ct)
    {
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

    /// <summary>
    /// Bot doğrulaması burada da denetlenir. Asıl doğrulamayı API yapar; buradaki erken ret onun
    /// yerine geçmez, yalnızca jeton hiç gönderilmemiş istekleri ağa çıkmadan eler.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact([FromForm] ContactFormModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { message = "Lütfen tüm alanları doldurun." });

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