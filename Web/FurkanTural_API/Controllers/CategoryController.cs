using FurkanTural_Application.DTOs.Category;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_API.Controllers.Base;
using FurkanTural_API.Models.Category;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace FurkanTural_API.Controllers;

[ApiVersion("1.0")]
public class CategoryController(ICategoryService categoryService) : JwtBaseController
{
    private readonly ICategoryService _categoryService = categoryService;

    /// <summary>Kategoriyi ID ile getir</summary>
    [HttpGet("{id:int}")]
    [Authorize(Policy = "VisitorOrAbove")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        => ToActionResult(await _categoryService.GetByIdAsync(id, cancellationToken));

    /// <summary>Tüm kategorileri listele</summary>
    [HttpGet]
    [Authorize(Policy = "VisitorOrAbove")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        => ToActionResult(await _categoryService.GetAllAsync(cancellationToken));

    /// <summary>Tüm kategorileri (admin) listele</summary>
    [HttpGet("admin")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAllForAdmin(CancellationToken cancellationToken)
        => ToActionResult(await _categoryService.GetAllForAdminAsync(cancellationToken));

    /// <summary>Kategoriyi ID ile getir (admin)</summary>
    [HttpGet("admin/{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetByIdForAdmin(int id, CancellationToken cancellationToken)
        => ToActionResult(await _categoryService.GetByIdForAdminAsync(id, cancellationToken));

    /// <summary>Kategorileri sayfalı listele</summary>
    [HttpGet("paged")]
    [Authorize(Policy = "VisitorOrAbove")]
    public async Task<IActionResult> GetPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
        => ToActionResult(await _categoryService.GetAllPagedAsync(pageNumber, pageSize, cancellationToken));

    /// <summary>Yeni kategori ekle</summary>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request, CancellationToken cancellationToken)
        => ToActionResult(await _categoryService.CreateAsync(new CreateCategoryDto
        {
            Name = request.Name,
            Color = request.Color,
            CreatedBy = SortUserId()
        }, cancellationToken));

    /// <summary>Kategoriyi güncelle</summary>
    [HttpPut]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Update([FromBody] UpdateCategoryRequest request, CancellationToken cancellationToken)
        => ToActionResult(await _categoryService.UpdateAsync(new UpdateCategoryDto
        {
            Id = request.Id,
            Name = request.Name,
            Color = request.Color,
            UpdatedBy = SortUserId()
        }, cancellationToken));

    /// <summary>Kategoriyi sil</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        => ToActionResult(await _categoryService.DeleteAsync(id, SortUserId(), cancellationToken));

    /// <summary>Kategorinin aktiflik durumunu değiştir</summary>
    [HttpPatch("{id:int}/toggle-active")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken cancellationToken)
        => ToActionResult(await _categoryService.ToggleActiveAsync(id, SortUserId(), cancellationToken));

    /// <summary>Silinen kategoriyi geri yükle</summary>
    [HttpPatch("{id:int}/restore")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken)
        => ToActionResult(await _categoryService.RestoreAsync(id, SortUserId(), cancellationToken));

    /// <summary>Yönetici paneli için kategori özetini getir (toplam + son işlem tarihi)</summary>
    [HttpGet("admin/summary")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAdminSummary(CancellationToken cancellationToken)
        => ToActionResult(await _categoryService.GetAdminSummaryAsync(cancellationToken));
}
