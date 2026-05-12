using Asp.Versioning;
using FurkanTural_Application.DTOs.ProjectImage;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_API.Controllers.Base;
using FurkanTural_API.Models.ProjectImage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_API.Controllers;

[ApiVersion("1.0")]
public class ProjectImageController(IProjectImageService projectImageService, IFileService fileService) : JwtBaseController
{
    private readonly IProjectImageService _projectImageService = projectImageService;
    private readonly IFileService _fileService = fileService;

    /// <summary>
    /// Proje görselini ID ile getir
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        => ToActionResult(await _projectImageService.GetByIdAsync(id, cancellationToken));

    /// <summary>
    /// Tüm proje görsellerini listele
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        => ToActionResult(await _projectImageService.GetAllAsync(cancellationToken));

    /// <summary>
    /// Admin için tüm proje görsellerini listele (soft-deleted dahil)
    /// </summary>
    [HttpGet("admin")]
    [Authorize]
    public async Task<IActionResult> GetAllForAdmin(CancellationToken cancellationToken)
        => ToActionResult(await _projectImageService.GetAllForAdminAsync(cancellationToken));

    /// <summary>
    /// Proje görselini yönetici paneli için ID ile getir (silinmiş dahil)
    /// </summary>
    [HttpGet("admin/{id:int}")]
    [Authorize]
    public async Task<IActionResult> GetByIdForAdmin(int id, CancellationToken cancellationToken)
        => ToActionResult(await _projectImageService.GetByIdForAdminAsync(id, cancellationToken));

    /// <summary>
    /// Proje görselinin aktiflik durumunu değiştir
    /// </summary>
    [HttpPatch("{id:int}/toggle-active")]
    [Authorize]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken cancellationToken)
        => ToActionResult(await _projectImageService.ToggleActiveAsync(id, SortUserId(), cancellationToken));

    /// <summary>
    /// Silinmiş proje görselini geri yükle
    /// </summary>
    [HttpPatch("{id:int}/restore")]
    [Authorize]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken)
        => ToActionResult(await _projectImageService.RestoreAsync(id, SortUserId(), cancellationToken));

    /// <summary>
    /// Proje görsellerini sayfalı listele
    /// </summary>
    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
        => ToActionResult(await _projectImageService.GetAllPagedAsync(pageNumber, pageSize, cancellationToken));

    /// <summary>
    /// Belirli bir projeye ait görselleri getir
    /// </summary>
    [HttpGet("by-project/{projectId:int}")]
    public async Task<IActionResult> GetByProjectId(int projectId, CancellationToken cancellationToken)
        => ToActionResult(await _projectImageService.GetByProjectIdAsync(projectId, cancellationToken));

    /// <summary>
    /// Proje görseli yükle ve kaydet
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateProjectImageRequest request, CancellationToken cancellationToken)
    {
        if (request.ImageData.Length == 0)
            return BadRequest("ImageData boş olamaz.");

        var userId = SortUserId() ?? 0;
        var fileName = await _fileService.SaveAsync(request.ImageData, request.ImageName, "Project", request.ProjectId, userId);

        var dto = new CreateProjectImageDto
        {
            Url = fileName,
            AltText = request.AltText,
            IsCover = request.IsCover,
            ProjectId = request.ProjectId,
            CreatedBy = SortUserId()
        };

        return ToActionResult(await _projectImageService.CreateAsync(dto, cancellationToken));
    }

    /// <summary>
    /// Proje görselini güncelle
    /// </summary>
    [HttpPut]
    [Authorize]
    public async Task<IActionResult> Update([FromBody] UpdateProjectImageRequest request, CancellationToken cancellationToken)
    {
        var existing = await _projectImageService.GetByIdAsync(request.Id, cancellationToken);
        if (existing.IsFailure)
            return ToActionResult(existing);

        var existingData = existing.Data!;

        // Yeni görsel gönderilmediyse sadece metadata güncelle, dosya işlemi yapma
        if (request.ImageData is not { Length: > 0 })
        {
            var metaDto = new UpdateProjectImageDto
            {
                Id = request.Id,
                Url = existingData.Url,
                AltText = request.AltText,
                IsCover = request.IsCover,
                ProjectId = request.ProjectId,
                UpdatedBy = SortUserId()
            };
            return ToActionResult(await _projectImageService.UpdateAsync(metaDto, cancellationToken));
        }

        var userId = SortUserId() ?? 0;
        var oldUrl = existingData.Url;

        // Adım 1: Yeni dosyayı fiziksel olarak kaydet
        var newFileName = await _fileService.SaveAsync(
            request.ImageData, request.ImageName ?? string.Empty, "Project", request.ProjectId, userId);

        // Adım 2: Veritabanını yeni dosya adıyla güncelle
        var updateDto = new UpdateProjectImageDto
        {
            Id = request.Id,
            Url = newFileName,
            AltText = request.AltText,
            IsCover = request.IsCover,
            ProjectId = request.ProjectId,
            UpdatedBy = SortUserId()
        };

        var updateResult = await _projectImageService.UpdateAsync(updateDto, cancellationToken);
        if (updateResult.IsFailure)
        {
            // DB başarısız → yeni dosyayı diskten sil (disk rollback)
            try { await _fileService.DeleteAsync(newFileName); } catch { /* best-effort */ }
            return ToActionResult(updateResult);
        }

        // Adım 3: Eski dosyayı fiziksel olarak sil
        try
        {
            await _fileService.DeleteAsync(oldUrl);
        }
        catch
        {
            // Eski dosya silinemedi → DB'yi eski haline geri sar + yeni dosyayı sil
            var rollbackDto = new UpdateProjectImageDto
            {
                Id = existingData.Id,
                Url = oldUrl,
                AltText = existingData.AltText,
                IsCover = existingData.IsCover,
                ProjectId = existingData.ProjectId,
                UpdatedBy = SortUserId()
            };
            try { await _projectImageService.UpdateAsync(rollbackDto, cancellationToken); } catch { /* best-effort */ }
            try { await _fileService.DeleteAsync(newFileName); } catch { /* best-effort */ }
            return StatusCode(500, "Eski görsel dosyası silinemedi. İşlem geri alındı.");
        }

        return ToActionResult(updateResult);
    }

    /// <summary>
    /// Proje görselini sil
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        => ToActionResult(await _projectImageService.DeleteAsync(id, cancellationToken));

    /// <summary>
    /// Yönetici paneli için proje görseli özetini getir (toplam + son işlem tarihi)
    /// </summary>
    [HttpGet("admin/summary")]
    [Authorize]
    public async Task<IActionResult> GetAdminSummary(CancellationToken cancellationToken)
        => ToActionResult(await _projectImageService.GetAdminSummaryAsync(cancellationToken));
}
