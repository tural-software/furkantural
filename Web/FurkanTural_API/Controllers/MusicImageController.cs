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
public class MusicImageController : BaseApiController
{
    private readonly IMusicImageService _musicImageService;
    private readonly IWebHostEnvironment _environment;

    public MusicImageController(IMusicImageService musicImageService, IWebHostEnvironment environment)
    {
        _musicImageService = musicImageService;
        _environment = environment;
    }

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

    /// <summary>
    /// Belirli bir müziğe ait görselleri getir
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

        var userId = GetUserId();
        var fileName = await ImageUploadHelper.SaveAsync(request.ImageData, request.ImageName, userId, _environment.WebRootPath);

        var dto = new CreateMusicImageDto
        {
            Url = fileName,
            MusicId = request.MusicId
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
            var userId = GetUserId();
            fileName = await ImageUploadHelper.SaveAsync(request.ImageData, request.ImageName ?? string.Empty, userId, _environment.WebRootPath);
        }

        var dto = new UpdateMusicImageDto
        {
            Id = request.Id,
            Url = fileName ?? existing.Data!.Url,
            MusicId = request.MusicId
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

    private int GetUserId()
    {
        var sub = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
               ?? User.FindFirst("sub")?.Value;
        return int.TryParse(sub, out var id) ? id : 0;
    }
}
