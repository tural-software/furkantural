using Asp.Versioning;
using FurkanTural_API.Controllers.Base;
using FurkanTural_API.Models.Common;
using FurkanTural_API.Models.Tag;
using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.DTOs.Tag;
using FurkanTural_Application.Services.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_API.Controllers;

/// <summary>Etiket uçları; yetki dağılımı <see cref="CategoryController"/> ile aynıdır — okuma ziyaretçiye açık, yazma yöneticiye.</summary>
[ApiVersion("1.0")]
public class TagController(ITagService tagService) : JwtBaseController
{
    private readonly ITagService _tagService = tagService;

    /// <summary>Etiketi ID ile getir</summary>
    [HttpGet("{id:int}")]
    [Authorize(Policy = "VisitorOrAbove")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        => ToActionResult(await _tagService.GetByIdAsync(id, cancellationToken));

    /// <summary>Etiketi kalıcı adres parçasıyla (slug) getir</summary>
    [HttpGet("by-slug/{slug}")]
    [Authorize(Policy = "VisitorOrAbove")]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken cancellationToken)
        => ToActionResult(await _tagService.GetBySlugAsync(slug, cancellationToken));

    /// <summary>Tüm etiketleri listele</summary>
    [HttpGet]
    [Authorize(Policy = "VisitorOrAbove")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        => ToActionResult(await _tagService.GetAllAsync(cancellationToken));

    /// <summary>Yazısı olan etiketleri yazı sayısına göre azalan listele (etiket bulutu)</summary>
    [HttpGet("popular")]
    [Authorize(Policy = "VisitorOrAbove")]
    public async Task<IActionResult> GetPopular([FromQuery] int take = 0, CancellationToken cancellationToken = default)
        => ToActionResult(await _tagService.GetPopularAsync(take, cancellationToken));

    /// <summary>Etiketleri sayfalı listele</summary>
    [HttpGet("paged")]
    [Authorize(Policy = "VisitorOrAbove")]
    public async Task<IActionResult> GetPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
        => ToActionResult(await _tagService.GetAllPagedAsync(pageNumber, pageSize, cancellationToken));

    /// <summary>Tüm etiketleri (admin) listele</summary>
    [HttpGet("admin")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAllForAdmin(CancellationToken cancellationToken)
        => ToActionResult(await _tagService.GetAllForAdminAsync(cancellationToken));

    /// <summary>Etiketi ID ile getir (admin)</summary>
    [HttpGet("admin/{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetByIdForAdmin(int id, CancellationToken cancellationToken)
        => ToActionResult(await _tagService.GetByIdForAdminAsync(id, cancellationToken));

    /// <summary>Yeni etiket oluştur</summary>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create([FromBody] CreateTagRequest request, CancellationToken cancellationToken)
        => ToActionResult(await _tagService.CreateAsync(new CreateTagDto { Name = request.Name }, cancellationToken));

    /// <summary>Etiketi güncelle</summary>
    [HttpPut]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Update([FromBody] UpdateTagRequest request, CancellationToken cancellationToken)
        => ToActionResult(await _tagService.UpdateAsync(
            new UpdateTagDto { Id = request.Id, Name = request.Name, Slug = request.Slug }, cancellationToken));

    /// <summary>Etiketi sil</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        => ToActionResult(await _tagService.DeleteAsync(id, SortUserId(), cancellationToken));

    /// <summary>Etiketin aktiflik durumunu değiştir</summary>
    [HttpPatch("{id:int}/toggle-active")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken cancellationToken)
        => ToActionResult(await _tagService.ToggleActiveAsync(id, SortUserId(), cancellationToken));

    /// <summary>Silinen etiketi geri yükle</summary>
    [HttpPatch("{id:int}/restore")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken)
        => ToActionResult(await _tagService.RestoreAsync(id, SortUserId(), cancellationToken));

    /// <summary>Yönetici paneli için etiket özetini getir (toplam + son işlem tarihi)</summary>
    [HttpGet("admin/summary")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAdminSummary(CancellationToken cancellationToken)
        => ToActionResult(await _tagService.GetAdminSummaryAsync(cancellationToken));

    /// <summary>Yönetici paneli için süzülmüş ve sayfalı etiket listesi</summary>
    [HttpGet("admin/paged")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAdminPaged(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] bool? isDeleted,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
        => ToActionResult(await _tagService.GetAllForAdminPagedAsync(
            AdminListQuery.From(search, isActive, isDeleted, dateFrom, dateTo, pageNumber, pageSize), cancellationToken));

    /// <summary>Yönetici paneli için etiket durum sayaçları; süzgeçler sayfalı listeyle aynıdır</summary>
    [HttpGet("admin/counts")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAdminCounts(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] bool? isDeleted,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        CancellationToken cancellationToken = default)
        => ToActionResult(await _tagService.GetAdminStatusCountsAsync(
            AdminListQuery.From(search, isActive, isDeleted, dateFrom, dateTo), cancellationToken));

    /// <summary>Seçili kayıtlara tek istekte uygulanır: siler, geri yükler, aktife ya da pasife alır. Uygun durumda olmayan kayıtlar atlanır ve yanıtta listelenir; en çok 100 kimlik</summary>
    [HttpPost("admin/bulk")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Bulk([FromBody] BulkActionRequest request, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<BulkAction>(request.Action, ignoreCase: true, out var action))
            return BadRequest(new { success = false, statusCode = 400, errors = new[] { "Geçersiz toplu işlem türü." } });

        return ToActionResult(await _tagService.BulkAsync(action, request.Ids ?? [], SortUserId(), cancellationToken));
    }
}
