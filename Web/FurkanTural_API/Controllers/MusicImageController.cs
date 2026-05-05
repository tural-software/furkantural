using FurkanTural_Application.DTOs.MusicImage;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_API.Controllers.Base;
using FurkanTural_API.Helpers;
using FurkanTural_API.Models.MusicImage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace FurkanTural_API.Controllers;

[ApiVersion("1.0")]
public class MusicImageController(IMusicImageService musicImageService, IWebHostEnvironment environment) : JwtBaseController
{
    private readonly IMusicImageService _musicImageService = musicImageService;
    private readonly IWebHostEnvironment _environment = environment;

    /// <summary>
    /// Müzik görselini ID ile getir
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        => ToActionResult(await _musicImageService.GetByIdAsync(id, cancellationToken));

    /// <summary>
    /// Tüm müzik görsellerini listele
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        => ToActionResult(await _musicImageService.GetAllAsync(cancellationToken));

    /// <summary>
    /// Müzik görsellerini sayfalı listele
    /// </summary>
    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
        => ToActionResult(await _musicImageService.GetAllPagedAsync(pageNumber, pageSize, cancellationToken));

    /// <summary>    /// Tüm müzik görsellerini yönetici paneli için listele (silinmişler dahil)
    /// </summary>
    [HttpGet("admin")]
    [Authorize]
    public async Task<IActionResult> GetAllForAdmin(CancellationToken cancellationToken)
        => ToActionResult(await _musicImageService.GetAllForAdminAsync(cancellationToken));

    /// <summary>
    /// Müzik görselini yönetici paneli için ID ile getir (silinmiş dahil)
    /// </summary>
    [HttpGet("admin/{id:int}")]
    [Authorize]
    public async Task<IActionResult> GetByIdForAdmin(int id, CancellationToken cancellationToken)
        => ToActionResult(await _musicImageService.GetByIdForAdminAsync(id, cancellationToken));

    /// <summary>    /// Belirli bir müziğe ait görselleri getir
    /// </summary>
    [HttpGet("by-music/{musicId:int}")]
    public async Task<IActionResult> GetByMusicId(int musicId, CancellationToken cancellationToken)
        => ToActionResult(await _musicImageService.GetByMusicIdAsync(musicId, cancellationToken));

    /// <summary>
    /// Müzik görseli yükle ve kaydet
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateMusicImageRequest request, CancellationToken cancellationToken)
    {
        if (request.ImageData.Length == 0)
            return BadRequest("ImageData boş olamaz.");

        var userId = SortUserId() ?? 0;
        var fileName = await ImageUploadHelper.SaveAsync(request.ImageData, request.ImageName, userId, _environment.WebRootPath);

        var dto = new CreateMusicImageDto
        {
            Url = fileName,
            MusicId = request.MusicId,
            CreatedBy = SortUserId()
        };

        return ToActionResult(await _musicImageService.CreateAsync(dto, cancellationToken));
    }

    /// <summary>
    /// Müzik görselini güncelle
    /// </summary>
    [HttpPut]
    [Authorize]
    public async Task<IActionResult> Update([FromBody] UpdateMusicImageRequest request, CancellationToken cancellationToken)
    {
        var existing = await _musicImageService.GetByIdAsync(request.Id, cancellationToken);
        if (existing.IsFailure)
            return ToActionResult(existing);

        string? fileName = null;
        if (request.ImageData is { Length: > 0 })
        {
            var userId = SortUserId() ?? 0;
            fileName = await ImageUploadHelper.SaveAsync(request.ImageData, request.ImageName ?? string.Empty, userId, _environment.WebRootPath);
        }

        var dto = new UpdateMusicImageDto
        {
            Id = request.Id,
            Url = fileName ?? existing.Data!.Url,
            MusicId = request.MusicId,
            UpdatedBy = SortUserId()
        };

        return ToActionResult(await _musicImageService.UpdateAsync(dto, cancellationToken));
    }

    /// <summary>
    /// Müzik görselini sil
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        => ToActionResult(await _musicImageService.DeleteAsync(id, cancellationToken));

    /// <summary>
    /// Müzik görselinin aktiflik durumunu değiştir
    /// </summary>
    [HttpPatch("{id:int}/toggle-active")]
    [Authorize]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken cancellationToken)
        => ToActionResult(await _musicImageService.ToggleActiveAsync(id, SortUserId(), cancellationToken));

    /// <summary>
    /// Silinen müzik görselini geri yükle
    /// </summary>
    [HttpPatch("{id:int}/restore")]
    [Authorize]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken)
        => ToActionResult(await _musicImageService.RestoreAsync(id, SortUserId(), cancellationToken));
}