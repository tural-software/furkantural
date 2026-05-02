using FurkanTural_Application.DTOs.Music;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_API.Controllers.Base;
using FurkanTural_API.Models.Music;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace FurkanTural_API.Controllers;

[ApiVersion("1.0")]
public class MusicController : BaseApiController
{
    private readonly IMusicService _musicService;

    public MusicController(IMusicService musicService)
    {
        _musicService = musicService;
    }

    /// <summary>
    /// Müziği ID ile getir
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        => ToActionResult(await _musicService.GetByIdAsync(id, cancellationToken));

    /// <summary>
    /// Tüm müzikleri listele
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        => ToActionResult(await _musicService.GetAllAsync(cancellationToken));

    /// <summary>
    /// Müzikleri sayfalı listele
    /// </summary>
    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
        => ToActionResult(await _musicService.GetAllPagedAsync(pageNumber, pageSize, cancellationToken));

    /// <summary>
    /// Yeni müzik ekle
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateMusicRequest request, CancellationToken cancellationToken)
        => ToActionResult(await _musicService.CreateAsync(new CreateMusicDto
        {
            Name = request.Name,
            Artist = request.Artist,
            Productor = request.Productor,
            Album = request.Album,
            Genre = request.Genre,
            Lyrics = request.Lyrics,
            Duration = request.Duration,
            ReleaseDate = request.ReleaseDate
        }, cancellationToken));

    /// <summary>
    /// Müziği güncelle
    /// </summary>
    [HttpPut]
    [Authorize]
    public async Task<IActionResult> Update([FromBody] UpdateMusicRequest request, CancellationToken cancellationToken)
        => ToActionResult(await _musicService.UpdateAsync(new UpdateMusicDto
        {
            Id = request.Id,
            Name = request.Name,
            Artist = request.Artist,
            Productor = request.Productor,
            Album = request.Album,
            Genre = request.Genre,
            Lyrics = request.Lyrics,
            Duration = request.Duration,
            ReleaseDate = request.ReleaseDate
        }, cancellationToken));

    /// <summary>
    /// Müziği sil
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        => ToActionResult(await _musicService.DeleteAsync(id, cancellationToken));
}
