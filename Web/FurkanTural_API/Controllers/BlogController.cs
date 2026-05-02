using FurkanTural_Application.DTOs.Blog;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_API.Controllers.Base;
using FurkanTural_API.Models.Blog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace FurkanTural_API.Controllers;

[ApiVersion("1.0")]
public class BlogController : BaseApiController
{
    private readonly IBlogService _blogService;

    public BlogController(IBlogService blogService)
    {
        _blogService = blogService;
    }

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
    /// Blog yazılarını sayfalı listele
    /// </summary>
    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
        => ToActionResult(await _blogService.GetAllPagedAsync(pageNumber, pageSize, cancellationToken));

    /// <summary>
    /// Yeni blog yazısı oluştur
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateBlogRequest request, CancellationToken cancellationToken)
        => ToActionResult(await _blogService.CreateAsync(new CreateBlogDto
        {
            Title = request.Title,
            Content = request.Content
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
            Content = request.Content
        }, cancellationToken));

    /// <summary>
    /// Blog yazısını sil
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        => ToActionResult(await _blogService.DeleteAsync(id, cancellationToken));
}
