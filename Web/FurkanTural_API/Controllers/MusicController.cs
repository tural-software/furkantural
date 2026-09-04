using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.DTOs.Music;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_API.Controllers.Base;
using FurkanTural_API.Models.Music;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace FurkanTural_API.Controllers;

[ApiVersion("1.0")]
public class MusicController(IMusicService musicService) : JwtBaseController
{
    private readonly IMusicService _musicService = musicService;

    /// <summary>Müziği ID ile getir</summary>
    [HttpGet("{id:int}")]
    [Authorize(Policy = "VisitorOrAbove")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        => ToActionResult(await _musicService.GetByIdAsync(id, cancellationToken));

    /// <summary>Tüm müzikleri listele</summary>
    [HttpGet]
    [Authorize(Policy = "VisitorOrAbove")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        => ToActionResult(await _musicService.GetAllAsync(cancellationToken));

    /// <summary>Müzikleri sayfalı listele</summary>
    [HttpGet("paged")]
    [Authorize(Policy = "VisitorOrAbove")]
    public async Task<IActionResult> GetPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
        => ToActionResult(await _musicService.GetAllPagedAsync(pageNumber, pageSize, cancellationToken));

    /// <summary>Tüm müzikleri yönetici paneli için listele (silinmişler dahil)</summary>
    [HttpGet("admin")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAllForAdmin(CancellationToken cancellationToken)
        => ToActionResult(await _musicService.GetAllForAdminAsync(cancellationToken));

    /// <summary>Müzii yönetici paneli için ID ile getir (silinmiş dahil)</summary>
    [HttpGet("admin/{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetByIdForAdmin(int id, CancellationToken cancellationToken)
        => ToActionResult(await _musicService.GetByIdForAdminAsync(id, cancellationToken));

    /// <summary>Yeni müzik ekle</summary>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
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
            ReleaseDate = request.ReleaseDate,
            YouTubeMusicUrl = request.YouTubeMusicUrl,
            CreatedBy = SortUserId()
        }, cancellationToken));

    /// <summary>Müziği güncelle</summary>
    [HttpPut]
    [Authorize(Policy = "AdminOnly")]
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
            ReleaseDate = request.ReleaseDate,
            YouTubeMusicUrl = request.YouTubeMusicUrl,
            UpdatedBy = SortUserId()
        }, cancellationToken));

    /// <summary>Müziği sil</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        => ToActionResult(await _musicService.DeleteAsync(id, SortUserId(), cancellationToken));

    /// <summary>Müziin aktiflik durumunu değiştir</summary>
    [HttpPatch("{id:int}/toggle-active")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken cancellationToken)
        => ToActionResult(await _musicService.ToggleActiveAsync(id, SortUserId(), cancellationToken));

    /// <summary>Silinen müzii geri yükle</summary>
    [HttpPatch("{id:int}/restore")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken)
        => ToActionResult(await _musicService.RestoreAsync(id, SortUserId(), cancellationToken));

    /// <summary>Yönetici paneli için müzik özetini getir (toplam + son işlem tarihi)</summary>
    [HttpGet("admin/summary")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAdminSummary(CancellationToken cancellationToken)
        => ToActionResult(await _musicService.GetAdminSummaryAsync(cancellationToken));

    /// <summary>Yönetici paneli için süzülmüş ve sayfalı müzik listesi</summary>
    [HttpGet("admin/paged")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAdminPaged(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] bool? isDeleted,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] string? artist,
        [FromQuery] int? musicId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
        => ToActionResult(await _musicService.GetAllForAdminPagedAsync(
            AdminListQuery.From(search, isActive, isDeleted, dateFrom, dateTo, pageNumber, pageSize), artist, musicId, cancellationToken));

    /// <summary>Yönetici paneli için müzik durum sayaçları; süzgeçler sayfalı listeyle aynıdır</summary>
    [HttpGet("admin/counts")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAdminCounts(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] bool? isDeleted,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] string? artist,
        [FromQuery] int? musicId,
        CancellationToken cancellationToken = default)
        => ToActionResult(await _musicService.GetAdminStatusCountsAsync(
            AdminListQuery.From(search, isActive, isDeleted, dateFrom, dateTo), artist, musicId, cancellationToken));
}
