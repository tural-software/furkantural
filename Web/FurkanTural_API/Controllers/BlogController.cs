using FurkanTural_API.Models.Common;
using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.DTOs.Blog;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_API.Controllers.Base;
using FurkanTural_API.Models.Blog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace FurkanTural_API.Controllers;

[ApiVersion("1.0")]
public class BlogController(IBlogService blogService) : JwtBaseController
{
    private readonly IBlogService _blogService = blogService;

    /// <summary>Blog yazısını ID ile getir</summary>
    [HttpGet("{id:int}")]
    [Authorize(Policy = "VisitorOrAbove")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        => ToActionResult(await _blogService.GetByIdAsync(id, cancellationToken));

    /// <summary>Tüm blog yazılarını listele</summary>
    [HttpGet]
    [Authorize(Policy = "VisitorOrAbove")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        => ToActionResult(await _blogService.GetAllAsync(cancellationToken));

    /// <summary>Tüm blog yazılarını yönetici paneli için listele</summary>
    [HttpGet("admin")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAllForAdmin(CancellationToken cancellationToken)
        => ToActionResult(await _blogService.GetAllForAdminAsync(cancellationToken));

    /// <summary>Blog yazısını yönetici paneli için ID ile getir (silinmiş dahil)</summary>
    [HttpGet("admin/{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetByIdForAdmin(int id, CancellationToken cancellationToken)
        => ToActionResult(await _blogService.GetByIdForAdminAsync(id, cancellationToken));

    /// <summary>Blog yazılarını sayfalı listele</summary>
    [HttpGet("paged")]
    [Authorize(Policy = "VisitorOrAbove")]
    public async Task<IActionResult> GetPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] int? categoryId = null, [FromQuery] string? search = null, CancellationToken cancellationToken = default)
        => ToActionResult(await _blogService.GetPublishedPagedAsync(pageNumber, pageSize, categoryId, search, cancellationToken));

    /// <summary>Sitemap/SEO için yayınlı yazıların hafif listesi (Id + tarihler; içerik taşınmaz)</summary>
    [HttpGet("sitemap")]
    [Authorize(Policy = "VisitorOrAbove")]
    public async Task<IActionResult> GetSitemap(CancellationToken cancellationToken)
        => ToActionResult(await _blogService.GetSitemapAsync(cancellationToken));

    /// <summary>Blog yazısının aktiflik durumunu değiştir</summary>
    [HttpPatch("{id:int}/toggle-active")]
    [Authorize(Policy = "UserOrAdmin")]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken cancellationToken)
    {
        if (!await HasOwnershipOrAdmin(id, cancellationToken))
            return Forbid();
        return ToActionResult(await _blogService.ToggleActiveAsync(id, SortUserId(), cancellationToken));
    }

    /// <summary>Silinmiş blog yazısını geri yükle</summary>
    [HttpPatch("{id:int}/restore")]
    [Authorize(Policy = "UserOrAdmin")]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken)
    {
        if (!await HasOwnershipOrAdmin(id, cancellationToken))
            return Forbid();
        return ToActionResult(await _blogService.RestoreAsync(id, SortUserId(), cancellationToken));
    }

    /// <summary>Yeni blog yazısı oluştur</summary>
    [HttpPost]
    [Authorize(Policy = "UserOrAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateBlogRequest request, CancellationToken cancellationToken)
        => ToActionResult(await _blogService.CreateAsync(new CreateBlogDto
        {
            Title = request.Title,
            Content = request.Content,
            CategoryIds = request.CategoryIds,
            CreatedBy = SortUserId()
        }, cancellationToken));

    /// <summary>Blog yazısını güncelle</summary>
    [HttpPut]
    [Authorize(Policy = "UserOrAdmin")]
    public async Task<IActionResult> Update([FromBody] UpdateBlogRequest request, CancellationToken cancellationToken)
    {
        if (!await HasOwnershipOrAdmin(request.Id, cancellationToken))
            return Forbid();
        return ToActionResult(await _blogService.UpdateAsync(new UpdateBlogDto
        {
            Id = request.Id,
            Title = request.Title,
            Content = request.Content,
            CategoryIds = request.CategoryIds,
            UpdatedBy = SortUserId()
        }, cancellationToken));
    }

    /// <summary>Blog yazısını sil</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "UserOrAdmin")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        if (!await HasOwnershipOrAdmin(id, cancellationToken))
            return Forbid();
        return ToActionResult(await _blogService.DeleteAsync(id, SortUserId(), cancellationToken));
    }

    /// <summary>Yönetici paneli için blog yazısı özetini getir (toplam + son işlem tarihi)</summary>
    [HttpGet("admin/summary")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAdminSummary(CancellationToken cancellationToken)
        => ToActionResult(await _blogService.GetAdminSummaryAsync(cancellationToken));

    /// <summary>Yönetici paneli için süzülmüş ve sayfalı blog listesi; includeContent=false verilirse içerik sütunu veri tabanından hiç okunmaz ve boş döner</summary>
    [HttpGet("admin/paged")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAdminPaged(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] bool? isDeleted,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int? blogId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] bool includeContent = true,
        CancellationToken cancellationToken = default)
        => ToActionResult(await _blogService.GetAllForAdminPagedAsync(
            AdminListQuery.From(search, isActive, isDeleted, dateFrom, dateTo, pageNumber, pageSize), blogId, includeContent, cancellationToken));

    /// <summary>Yönetici paneli için blog durum sayaçları; süzgeçler sayfalı listeyle aynıdır</summary>
    [HttpGet("admin/counts")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAdminCounts(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] bool? isDeleted,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int? blogId,
        CancellationToken cancellationToken = default)
        => ToActionResult(await _blogService.GetAdminStatusCountsAsync(
            AdminListQuery.From(search, isActive, isDeleted, dateFrom, dateTo), blogId, cancellationToken));

    /// <summary>Açılır liste sözlüğü: silinmemiş kayıtların kimliği ve etiketi. Sayfa değil sözlüktür; take verilirse o kadar, verilmezse tümü döner</summary>
    [HttpGet("admin/options")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAdminOptions(
        [FromQuery] string? search,
        [FromQuery] int? take,
        CancellationToken cancellationToken = default)
        => ToActionResult(await _blogService.GetAdminOptionsAsync(search, take, cancellationToken));

    /// <summary>Seçili blogları tek istekte siler, geri yükler, aktife ya da pasife alır. Uygun durumda olmayan kayıtlar atlanır ve yanıtta listelenir; en çok 100 kimlik</summary>
    [HttpPost("admin/bulk")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Bulk([FromBody] BulkActionRequest request, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<BulkAction>(request.Action, ignoreCase: true, out var action))
            return BadRequest(new { success = false, statusCode = 400, errors = new[] { "Geçersiz toplu işlem türü." } });

        return ToActionResult(await _blogService.BulkAsync(action, request.Ids ?? [], SortUserId(), cancellationToken));
    }

    /// <summary>Yetkilendirme politikası yalnızca "üye ya da yönetici" der; kaydın kime ait olduğunu söylemez. Sahiplik bu yüzden ayrıca burada denetlenir ve yönetici koşulsuz geçer.</summary>
    private async Task<bool> HasOwnershipOrAdmin(int blogId, CancellationToken cancellationToken)
    {
        if (SortUserRole() == "Admin") return true;

        var userId = SortUserId();
        if (userId is null) return false;

        var entity = await _blogService.GetByIdForAdminAsync(blogId, cancellationToken);
        return entity.Success && entity.Data?.CreatedBy == userId;
    }
}
