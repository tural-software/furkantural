using FurkanTural_Application.DTOs.BlogImage;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_API.Controllers.Base;
using FurkanTural_API.Helpers;
using FurkanTural_API.Models.BlogImage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace FurkanTural_API.Controllers;

[ApiVersion("1.0")]
public class BlogImageController(IBlogImageService blogImageService, IWebHostEnvironment environment) : BaseApiController
{
    private readonly IBlogImageService _blogImageService = blogImageService;
    private readonly IWebHostEnvironment _environment = environment;

    /// <summary>
    /// Blog görselini ID ile getir
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        => ToActionResult(await _blogImageService.GetByIdAsync(id, cancellationToken));

    /// <summary>
    /// Tüm blog görsellerini listele
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        => ToActionResult(await _blogImageService.GetAllAsync(cancellationToken));

    /// <summary>
    /// Blog görsellerini sayfalı listele
    /// </summary>
    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
        => ToActionResult(await _blogImageService.GetAllPagedAsync(pageNumber, pageSize, cancellationToken));

    /// <summary>
    /// Belirli bir bloğa ait görselleri getir
    /// </summary>
    [HttpGet("by-blog/{blogId:int}")]
    public async Task<IActionResult> GetByBlogId(int blogId, CancellationToken cancellationToken)
        => ToActionResult(await _blogImageService.GetByBlogIdAsync(blogId, cancellationToken));

    /// <summary>
    /// Blog görseli yükle ve kaydet
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateBlogImageRequest request, CancellationToken cancellationToken)
    {
        if (request.ImageData.Length == 0)
            return BadRequest("ImageData boş olamaz.");

        var userId = GetUserId();
        var fileName = await ImageUploadHelper.SaveAsync(request.ImageData, request.ImageName, userId, _environment.WebRootPath);

        var dto = new CreateBlogImageDto
        {
            Url = fileName,
            AltText = request.AltText,
            IsCover = request.IsCover,
            BlogId = request.BlogId
        };

        return ToActionResult(await _blogImageService.CreateAsync(dto, cancellationToken));
    }

    /// <summary>
    /// Blog görselini güncelle
    /// </summary>
    [HttpPut]
    [Authorize]
    public async Task<IActionResult> Update([FromBody] UpdateBlogImageRequest request, CancellationToken cancellationToken)
    {
        var existing = await _blogImageService.GetByIdAsync(request.Id, cancellationToken);
        if (existing.IsFailure)
            return ToActionResult(existing);

        string? fileName = null;
        if (request.ImageData is { Length: > 0 })
        {
            var userId = GetUserId();
            fileName = await ImageUploadHelper.SaveAsync(request.ImageData, request.ImageName ?? string.Empty, userId, _environment.WebRootPath);
        }

        var dto = new UpdateBlogImageDto
        {
            Id = request.Id,
            Url = fileName ?? existing.Data!.Url,
            AltText = request.AltText,
            IsCover = request.IsCover,
            BlogId = request.BlogId
        };

        return ToActionResult(await _blogImageService.UpdateAsync(dto, cancellationToken));
    }

    /// <summary>
    /// Blog görselini sil
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        => ToActionResult(await _blogImageService.DeleteAsync(id, cancellationToken));

    private int GetUserId()
    {
        var sub = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
               ?? User.FindFirst("sub")?.Value;
        return int.TryParse(sub, out var id) ? id : 0;
    }
}