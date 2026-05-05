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

    /// <summary>
    /// Blog yazısını ID ile getir
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        => ToActionResult(await _blogService.GetByIdAsync(id, cancellationToken));

    /// <summary>
    /// Tüm blog yazılarını listele
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        => ToActionResult(await _blogService.GetAllAsync(cancellationToken));

    /// <summary>
    /// Tüm blog yazılarını yönetici paneli için listele
    /// </summary>
    [HttpGet("admin")]
    [Authorize]
    public async Task<IActionResult> GetAllForAdmin(CancellationToken cancellationToken)
        => ToActionResult(await _blogService.GetAllForAdminAsync(cancellationToken));

    /// <summary>    /// Blog yazısını yönetici paneli için ID ile getir (silinmiş dahil)
    /// </summary>
    [HttpGet("admin/{id:int}")]
    [Authorize]
    public async Task<IActionResult> GetByIdForAdmin(int id, CancellationToken cancellationToken)
        => ToActionResult(await _blogService.GetByIdForAdminAsync(id, cancellationToken));

    /// <summary>    /// Blog yazılarını sayfalı listele
    /// </summary>
    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
        => ToActionResult(await _blogService.GetAllPagedAsync(pageNumber, pageSize, cancellationToken));

    /// <summary>
    /// Blog yazısının aktiflik durumunu değiştir
    /// </summary>
    [HttpPatch("{id:int}/toggle-active")]
    [Authorize]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken cancellationToken)
        => ToActionResult(await _blogService.ToggleActiveAsync(id, SortUserId(), cancellationToken));

    /// <summary>
    /// Silinmiş blog yazısını geri yükle
    /// </summary>
    [HttpPatch("{id:int}/restore")]
    [Authorize]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken)
        => ToActionResult(await _blogService.RestoreAsync(id, SortUserId(), cancellationToken));

    /// <summary>
    /// Yeni blog yazısı oluştur
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateBlogRequest request, CancellationToken cancellationToken)
        => ToActionResult(await _blogService.CreateAsync(new CreateBlogDto
        {
            Title = request.Title,
            Content = request.Content,
            CreatedBy = SortUserId()
        }, cancellationToken));

    /// <summary>
    /// Blog yazısını güncelle
    /// </summary>
    [HttpPut]
    [Authorize]
    public async Task<IActionResult> Update([FromBody] UpdateBlogRequest request, CancellationToken cancellationToken)
        => ToActionResult(await _blogService.UpdateAsync(new UpdateBlogDto
        {
            Id = request.Id,
            Title = request.Title,
            Content = request.Content,
            UpdatedBy = SortUserId()
        }, cancellationToken));

    /// <summary>
    /// Blog yazısını sil
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        => ToActionResult(await _blogService.DeleteAsync(id, cancellationToken));
}