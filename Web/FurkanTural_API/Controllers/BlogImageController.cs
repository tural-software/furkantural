using FurkanTural_Application.DTOs.BlogImage;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_API.Controllers.Base;
using FurkanTural_API.Models.BlogImage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace FurkanTural_API.Controllers;

/// <summary>Görsel değiştirme üç adımda yürür ve her adımın geri alması vardır: yeni dosya diske yazılır, kayıt yeni ada güncellenir, sonra eski dosya silinir. Veri tabanı adımı düşerse yeni dosya diskten kaldırılır. Eski dosya silinemezse kayıt eski adına geri sarılır, yeni dosya silinir ve istek 500 döner: silinemeyen tek bir dosya, tamamlanmış görünen bir güncellemeyi bilerek geri aldırır, çünkü kayıtla disk arasında sessiz bir ayrışma bırakmaktansa işlemi hiç yapmamak yeğlenir.<para>Geri alma denemeleri kendi başlarına yutulur; zaten hata dönmekte olan bir istek, temizlik de başarısız oldu diye ikinci bir istisnayla bölünmez.</para><para>İstek görsel verisi taşımıyorsa dosya katmanına hiç uğranmaz, yalnızca üst veri güncellenir.</para></summary>
[ApiVersion("1.0")]
public class BlogImageController(IBlogImageService blogImageService, IFileService fileService) : JwtBaseController
{
    private readonly IBlogImageService _blogImageService = blogImageService;
    private readonly IFileService _fileService = fileService;

    /// <summary>Blog görselini ID ile getir</summary>
    [HttpGet("{id:int}")]
    [Authorize(Policy = "VisitorOrAbove")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        => ToActionResult(await _blogImageService.GetByIdAsync(id, cancellationToken));

    /// <summary>Tüm blog görsellerini listele</summary>
    [HttpGet]
    [Authorize(Policy = "VisitorOrAbove")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        => ToActionResult(await _blogImageService.GetAllAsync(cancellationToken));

    /// <summary>Admin için tüm blog görsellerini listele (soft-deleted dahil)</summary>
    [HttpGet("admin")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAllForAdmin(CancellationToken cancellationToken)
        => ToActionResult(await _blogImageService.GetAllForAdminAsync(cancellationToken));

    /// <summary>Blog görselini yönetici paneli için ID ile getir (silinmiş dahil)</summary>
    [HttpGet("admin/{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetByIdForAdmin(int id, CancellationToken cancellationToken)
        => ToActionResult(await _blogImageService.GetByIdForAdminAsync(id, cancellationToken));

    /// <summary>Blog görselinin aktiflik durumunu değiştir</summary>
    [HttpPatch("{id:int}/toggle-active")]
    [Authorize(Policy = "UserOrAdmin")]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken cancellationToken)
    {
        if (!await HasOwnershipOrAdmin(id, cancellationToken))
            return Forbid();
        return ToActionResult(await _blogImageService.ToggleActiveAsync(id, SortUserId(), cancellationToken));
    }

    /// <summary>Silinmiş blog görselini geri yükle</summary>
    [HttpPatch("{id:int}/restore")]
    [Authorize(Policy = "UserOrAdmin")]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken)
    {
        if (!await HasOwnershipOrAdmin(id, cancellationToken))
            return Forbid();
        return ToActionResult(await _blogImageService.RestoreAsync(id, SortUserId(), cancellationToken));
    }

    /// <summary>Blog görsellerini sayfalı listele</summary>
    [HttpGet("paged")]
    [Authorize(Policy = "VisitorOrAbove")]
    public async Task<IActionResult> GetPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
        => ToActionResult(await _blogImageService.GetAllPagedAsync(pageNumber, pageSize, cancellationToken));

    /// <summary>Belirli bir bloğa ait görselleri getir</summary>
    [HttpGet("by-blog/{blogId:int}")]
    [Authorize(Policy = "VisitorOrAbove")]
    public async Task<IActionResult> GetByBlogId(int blogId, CancellationToken cancellationToken)
        => ToActionResult(await _blogImageService.GetByBlogIdAsync(blogId, cancellationToken));

    /// <summary>Blog görseli yükle ve kaydet</summary>
    [HttpPost]
    [Authorize(Policy = "UserOrAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateBlogImageRequest request, CancellationToken cancellationToken)
    {
        if (request.ImageData.Length == 0)
            return BadRequest("ImageData boş olamaz.");

        var userId = SortUserId() ?? 0;
        var fileName = await _fileService.SaveAsync(request.ImageData, request.ImageName, "Blog", request.BlogId, userId);

        var dto = new CreateBlogImageDto
        {
            Url = fileName,
            AltText = request.AltText,
            IsCover = request.IsCover,
            BlogId = request.BlogId,
            CreatedBy = SortUserId()
        };

        return ToActionResult(await _blogImageService.CreateAsync(dto, cancellationToken));
    }

    /// <summary>Blog görselini güncelle</summary>
    [HttpPut]
    [Authorize(Policy = "UserOrAdmin")]
    public async Task<IActionResult> Update([FromBody] UpdateBlogImageRequest request, CancellationToken cancellationToken)
    {
        if (!await HasOwnershipOrAdmin(request.Id, cancellationToken))
            return Forbid();

        var existing = await _blogImageService.GetByIdAsync(request.Id, cancellationToken);
        if (existing.IsFailure)
            return ToActionResult(existing);

        var existingData = existing.Data!;

        if (request.ImageData is not { Length: > 0 })
        {
            var metaDto = new UpdateBlogImageDto
            {
                Id = request.Id,
                Url = existingData.Url,
                AltText = request.AltText,
                IsCover = request.IsCover,
                BlogId = request.BlogId,
                UpdatedBy = SortUserId()
            };
            return ToActionResult(await _blogImageService.UpdateAsync(metaDto, cancellationToken));
        }

        var userId = SortUserId() ?? 0;
        var oldUrl = existingData.Url;

        var newFileName = await _fileService.SaveAsync(
            request.ImageData, request.ImageName ?? string.Empty, "Blog", request.BlogId, userId);

        var updateDto = new UpdateBlogImageDto
        {
            Id = request.Id,
            Url = newFileName,
            AltText = request.AltText,
            IsCover = request.IsCover,
            BlogId = request.BlogId,
            UpdatedBy = SortUserId()
        };

        var updateResult = await _blogImageService.UpdateAsync(updateDto, cancellationToken);
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
            var rollbackDto = new UpdateBlogImageDto
            {
                Id = existingData.Id,
                Url = oldUrl,
                AltText = existingData.AltText,
                IsCover = existingData.IsCover,
                BlogId = existingData.BlogId,
                UpdatedBy = SortUserId()
            };
            try { await _blogImageService.UpdateAsync(rollbackDto, cancellationToken); } catch { }
            try { await _fileService.DeleteAsync(newFileName); } catch { }
            return StatusCode(500, "Eski görsel dosyası silinemedi. İşlem geri alındı.");
        }

        return ToActionResult(updateResult);
    }

    /// <summary>Blog görselini sil</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "UserOrAdmin")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        if (!await HasOwnershipOrAdmin(id, cancellationToken))
            return Forbid();
        return ToActionResult(await _blogImageService.DeleteAsync(id, SortUserId(), cancellationToken));
    }

    /// <summary>Yönetici paneli için blog görseli özetini getir (toplam + son işlem tarihi)</summary>
    [HttpGet("admin/summary")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAdminSummary(CancellationToken cancellationToken)
        => ToActionResult(await _blogImageService.GetAdminSummaryAsync(cancellationToken));

    /// <summary>Sahiplik görselin kendisinden değil bağlı olduğu yazıdan çözülür; yetkilendirme politikası yalnızca rolü bildiği için bu denetim ayrıca yapılır ve yönetici koşulsuz geçer.</summary>
    private async Task<bool> HasOwnershipOrAdmin(int imageId, CancellationToken cancellationToken)
    {
        if (SortUserRole() == "Admin") return true;

        var userId = SortUserId();
        if (userId is null) return false;

        var entity = await _blogImageService.GetByIdForAdminAsync(imageId, cancellationToken);
        return entity.Success && entity.Data?.CreatedBy == userId;
    }
}
