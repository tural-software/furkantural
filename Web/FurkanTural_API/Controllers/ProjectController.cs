using FurkanTural_API.Models.Common;
using FurkanTural_Application.DTOs.Common;
using Asp.Versioning;
using FurkanTural_Application.DTOs.Project;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_API.Controllers.Base;
using FurkanTural_API.Models.Project;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_API.Controllers;

[ApiVersion("1.0")]
public class ProjectController(IProjectService projectService) : JwtBaseController
{
    private readonly IProjectService _projectService = projectService;

    /// <summary>Projeyi ID ile getir</summary>
    [HttpGet("{id:int}")]
    [Authorize(Policy = "VisitorOrAbove")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        => ToActionResult(await _projectService.GetByIdAsync(id, cancellationToken));

    /// <summary>Tüm projeleri listele</summary>
    [HttpGet]
    [Authorize(Policy = "VisitorOrAbove")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        => ToActionResult(await _projectService.GetAllAsync(cancellationToken));

    /// <summary>Tüm projeleri yönetici paneli için listele</summary>
    [HttpGet("admin")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAllForAdmin(CancellationToken cancellationToken)
        => ToActionResult(await _projectService.GetAllForAdminAsync(cancellationToken));

    /// <summary>Projeyi yönetici paneli için ID ile getir (silinmiş dahil)</summary>
    [HttpGet("admin/{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetByIdForAdmin(int id, CancellationToken cancellationToken)
        => ToActionResult(await _projectService.GetByIdForAdminAsync(id, cancellationToken));

    /// <summary>Projeleri sayfalı listele</summary>
    [HttpGet("paged")]
    [Authorize(Policy = "VisitorOrAbove")]
    public async Task<IActionResult> GetPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
        => ToActionResult(await _projectService.GetAllPagedAsync(pageNumber, pageSize, cancellationToken));

    /// <summary>Projenin aktiflik durumunu değiştir</summary>
    [HttpPatch("{id:int}/toggle-active")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken cancellationToken)
        => ToActionResult(await _projectService.ToggleActiveAsync(id, SortUserId(), cancellationToken));

    /// <summary>Silinmiş projeyi geri yükle</summary>
    [HttpPatch("{id:int}/restore")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken)
        => ToActionResult(await _projectService.RestoreAsync(id, SortUserId(), cancellationToken));

    /// <summary>Yeni proje oluştur</summary>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create([FromBody] CreateProjectRequest request, CancellationToken cancellationToken)
        => ToActionResult(await _projectService.CreateAsync(new CreateProjectDto
        {
            Title = request.Title,
            Description = request.Description,
            ShortDescription = request.ShortDescription,
            TechStack = request.TechStack,
            GitHubUrl = request.GitHubUrl,
            DemoUrl = request.DemoUrl,
            IsCompleted = request.IsCompleted,
            CreatedBy = SortUserId()
        }, cancellationToken));

    /// <summary>Projeyi güncelle</summary>
    [HttpPut]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Update([FromBody] UpdateProjectRequest request, CancellationToken cancellationToken)
        => ToActionResult(await _projectService.UpdateAsync(new UpdateProjectDto
        {
            Id = request.Id,
            Title = request.Title,
            Description = request.Description,
            ShortDescription = request.ShortDescription,
            TechStack = request.TechStack,
            GitHubUrl = request.GitHubUrl,
            DemoUrl = request.DemoUrl,
            IsCompleted = request.IsCompleted,
            UpdatedBy = SortUserId()
        }, cancellationToken));

    /// <summary>Projeyi sil</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        => ToActionResult(await _projectService.DeleteAsync(id, SortUserId(), cancellationToken));

    /// <summary>Yönetici paneli için proje özetini getir (toplam + son işlem tarihi)</summary>
    [HttpGet("admin/summary")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAdminSummary(CancellationToken cancellationToken)
        => ToActionResult(await _projectService.GetAdminSummaryAsync(cancellationToken));

    /// <summary>Yönetici paneli için süzülmüş ve sayfalı proje listesi</summary>
    [HttpGet("admin/paged")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAdminPaged(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] bool? isDeleted,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] bool? isCompleted,
        [FromQuery] int? projectId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
        => ToActionResult(await _projectService.GetAllForAdminPagedAsync(
            AdminListQuery.From(search, isActive, isDeleted, dateFrom, dateTo, pageNumber, pageSize), isCompleted, projectId, cancellationToken));

    /// <summary>Yönetici paneli için proje durum sayaçları; süzgeçler sayfalı listeyle aynıdır</summary>
    [HttpGet("admin/counts")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAdminCounts(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] bool? isDeleted,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] bool? isCompleted,
        [FromQuery] int? projectId,
        CancellationToken cancellationToken = default)
        => ToActionResult(await _projectService.GetAdminStatusCountsAsync(
            AdminListQuery.From(search, isActive, isDeleted, dateFrom, dateTo), isCompleted, projectId, cancellationToken));

    /// <summary>Açılır liste sözlüğü: silinmemiş kayıtların kimliği ve etiketi. Sayfa değil sözlüktür; take verilirse o kadar, verilmezse tümü döner</summary>
    [HttpGet("admin/options")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAdminOptions(
        [FromQuery] string? search,
        [FromQuery] int? take,
        CancellationToken cancellationToken = default)
        => ToActionResult(await _projectService.GetAdminOptionsAsync(search, take, cancellationToken));

    /// <summary>Seçili kayıtlara tek istekte uygulanır: siler, geri yükler, aktife ya da pasife alır. Uygun durumda olmayan kayıtlar atlanır ve yanıtta listelenir; en çok 100 kimlik</summary>
    [HttpPost("admin/bulk")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Bulk([FromBody] BulkActionRequest request, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<BulkAction>(request.Action, ignoreCase: true, out var action))
            return BadRequest(new { success = false, statusCode = 400, errors = new[] { "Geçersiz toplu işlem türü." } });

        return ToActionResult(await _projectService.BulkAsync(action, request.Ids ?? [], SortUserId(), cancellationToken));
    }
}
