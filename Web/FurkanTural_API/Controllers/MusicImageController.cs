using FurkanTural_Application.DTOs.MusicImage;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_API.Controllers.Base;
using FurkanTural_API.Models.MusicImage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace FurkanTural_API.Controllers;

[ApiVersion("1.0")]
public class MusicImageController(IMusicImageService musicImageService, IFileService fileService) : JwtBaseController
{
    private readonly IMusicImageService _musicImageService = musicImageService;
    private readonly IFileService _fileService = fileService;

    /// <summary>
    /// Müzik görselini ID ile getir
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize(Policy = "VisitorOrAbove")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        => ToActionResult(await _musicImageService.GetByIdAsync(id, cancellationToken));

    /// <summary>
    /// Tüm müzik görsellerini listele
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "VisitorOrAbove")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        => ToActionResult(await _musicImageService.GetAllAsync(cancellationToken));

    /// <summary>
    /// Müzik görsellerini sayfalı listele
    /// </summary>
    [HttpGet("paged")]
    [Authorize(Policy = "VisitorOrAbove")]
    public async Task<IActionResult> GetPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
        => ToActionResult(await _musicImageService.GetAllPagedAsync(pageNumber, pageSize, cancellationToken));

    /// <summary>    /// Tüm müzik görsellerini yönetici paneli için listele (silinmişler dahil)
    /// </summary>
    [HttpGet("admin")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAllForAdmin(CancellationToken cancellationToken)
        => ToActionResult(await _musicImageService.GetAllForAdminAsync(cancellationToken));

    /// <summary>
    /// Müzik görselini yönetici paneli için ID ile getir (silinmiş dahil)
    /// </summary>
    [HttpGet("admin/{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetByIdForAdmin(int id, CancellationToken cancellationToken)
        => ToActionResult(await _musicImageService.GetByIdForAdminAsync(id, cancellationToken));

    /// <summary>    /// Belirli bir müziğe ait görselleri getir
    /// </summary>
    [HttpGet("by-music/{musicId:int}")]
    [Authorize(Policy = "VisitorOrAbove")]
    public async Task<IActionResult> GetByMusicId(int musicId, CancellationToken cancellationToken)
        => ToActionResult(await _musicImageService.GetByMusicIdAsync(musicId, cancellationToken));

    /// <summary>
    /// Müzik görseli yükle ve kaydet
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create([FromBody] CreateMusicImageRequest request, CancellationToken cancellationToken)
    {
        if (request.ImageData.Length == 0)
            return BadRequest("ImageData boş olamaz.");

        var userId = SortUserId() ?? 0;
        var fileName = await _fileService.SaveAsync(request.ImageData, request.ImageName, "Music", request.MusicId, userId);

        var dto = new CreateMusicImageDto
        {
            Url = fileName,
            AltText = request.AltText,
            IsCover = request.IsCover,
            MusicId = request.MusicId,
            CreatedBy = SortUserId()
        };

        return ToActionResult(await _musicImageService.CreateAsync(dto, cancellationToken));
    }

    /// <summary>
    /// Müzik görselini güncelle
    /// </summary>
    [HttpPut]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Update([FromBody] UpdateMusicImageRequest request, CancellationToken cancellationToken)
    {
        var existing = await _musicImageService.GetByIdAsync(request.Id, cancellationToken);
        if (existing.IsFailure)
            return ToActionResult(existing);

        var existingData = existing.Data!;

        if (request.ImageData is not { Length: > 0 })
        {
            var metaDto = new UpdateMusicImageDto
            {
                Id = request.Id,
                Url = existingData.Url,
                AltText = request.AltText,
                IsCover = request.IsCover,
                MusicId = request.MusicId,
                UpdatedBy = SortUserId()
            };
            return ToActionResult(await _musicImageService.UpdateAsync(metaDto, cancellationToken));
        }

        var userId = SortUserId() ?? 0;
        var oldUrl = existingData.Url;

        var newFileName = await _fileService.SaveAsync(
            request.ImageData, request.ImageName ?? string.Empty, "Music", request.MusicId, userId);

        var updateDto = new UpdateMusicImageDto
        {
            Id = request.Id,
            Url = newFileName,
            AltText = request.AltText,
            IsCover = request.IsCover,
            MusicId = request.MusicId,
            UpdatedBy = SortUserId()
        };

        var updateResult = await _musicImageService.UpdateAsync(updateDto, cancellationToken);
        if (updateResult.IsFailure)
        {
            try { await _fileService.DeleteAsync(newFileName); } catch { }
            return ToActionResult(updateResult);
        }

        try
        {
            await _fileService.DeleteAsync(oldUrl);
        }
        catch
        {
            var rollbackDto = new UpdateMusicImageDto
            {
                Id = existingData.Id,
                Url = oldUrl,
                AltText = existingData.AltText,
                IsCover = existingData.IsCover,
                MusicId = existingData.MusicId,
                UpdatedBy = SortUserId()
            };
            try { await _musicImageService.UpdateAsync(rollbackDto, cancellationToken); } catch { }
            try { await _fileService.DeleteAsync(newFileName); } catch { }
            return StatusCode(500, "Eski görsel dosyası silinemedi. İşlem geri alındı.");
        }

        return ToActionResult(updateResult);
    }

    /// <summary>
    /// Müzik görselini sil
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        => ToActionResult(await _musicImageService.DeleteAsync(id, cancellationToken));

    /// <summary>
    /// Müzik görselinin aktiflik durumunu değiştir
    /// </summary>
    [HttpPatch("{id:int}/toggle-active")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken cancellationToken)
        => ToActionResult(await _musicImageService.ToggleActiveAsync(id, SortUserId(), cancellationToken));

    /// <summary>
    /// Silinen müzik görselini geri yükle
    /// </summary>
    [HttpPatch("{id:int}/restore")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken)
        => ToActionResult(await _musicImageService.RestoreAsync(id, SortUserId(), cancellationToken));

    /// <summary>
    /// Yönetici paneli için müzik görseli özetini getir (toplam + son işlem tarihi)
    /// </summary>
    [HttpGet("admin/summary")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAdminSummary(CancellationToken cancellationToken)
        => ToActionResult(await _musicImageService.GetAdminSummaryAsync(cancellationToken));
}