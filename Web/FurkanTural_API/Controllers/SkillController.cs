using FurkanTural_Application.DTOs.Skill;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_API.Controllers.Base;
using FurkanTural_API.Models.Skill;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace FurkanTural_API.Controllers;

[ApiVersion("1.0")]
public class SkillController(ISkillService skillService) : JwtBaseController
{
    private readonly ISkillService _skillService = skillService;
    
    /// <summary>
    /// Yetkinliği ID ile getir
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        => ToActionResult(await _skillService.GetByIdAsync(id, cancellationToken));

    /// <summary>
    /// Tüm yetkinlikleri listele
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        => ToActionResult(await _skillService.GetAllAsync(cancellationToken));

    /// <summary>
    /// Tüm yetkinlikleri (admin) listele
    /// </summary>
    [HttpGet("admin")]
    [Authorize]
    public async Task<IActionResult> GetAllForAdmin(CancellationToken cancellationToken)
        => ToActionResult(await _skillService.GetAllForAdminAsync(cancellationToken));

    /// <summary>
    /// Yetkinliği ID ile getir (admin)
    /// </summary>
    [HttpGet("admin/{id:int}")]
    [Authorize]
    public async Task<IActionResult> GetByIdForAdmin(int id, CancellationToken cancellationToken)
        => ToActionResult(await _skillService.GetByIdForAdminAsync(id, cancellationToken));

    /// <summary>
    /// Yetkinlikleri sayfalı listele
    /// </summary>
    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
        => ToActionResult(await _skillService.GetAllPagedAsync(pageNumber, pageSize, cancellationToken));

    /// <summary>
    /// Yeni yetkinlik ekle
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateSkillRequest request, CancellationToken cancellationToken)
        => ToActionResult(await _skillService.CreateAsync(new CreateSkillDto
        {
            Name = request.Name,
            Proficiency = request.Proficiency,
            CreatedBy = SortUserId()
        }, cancellationToken));

    /// <summary>
    /// Yetkinliği güncelle
    /// </summary>
    [HttpPut]
    [Authorize]
    public async Task<IActionResult> Update([FromBody] UpdateSkillRequest request, CancellationToken cancellationToken)
        => ToActionResult(await _skillService.UpdateAsync(new UpdateSkillDto
        {
            Id = request.Id,
            Name = request.Name,
            Proficiency = request.Proficiency,
            UpdatedBy = SortUserId()
        }, cancellationToken));

    /// <summary>
    /// Yetkinliği sil
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        => ToActionResult(await _skillService.DeleteAsync(id, cancellationToken));

    /// <summary>
    /// Yetkinliğin aktiflik durumunu değiştir
    /// </summary>
    [HttpPatch("{id:int}/toggle-active")]
    [Authorize]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken cancellationToken)
        => ToActionResult(await _skillService.ToggleActiveAsync(id, SortUserId(), cancellationToken));

    /// <summary>
    /// Silinen yetkinliği geri yükle
    /// </summary>
    [HttpPatch("{id:int}/restore")]
    [Authorize]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken)
        => ToActionResult(await _skillService.RestoreAsync(id, SortUserId(), cancellationToken));
}