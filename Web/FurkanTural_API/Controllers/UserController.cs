using FurkanTural_Application.DTOs.User;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_API.Controllers.Base;
using FurkanTural_API.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace FurkanTural_API.Controllers;

[Authorize]
[ApiVersion("1.0")]
public class UserController : JwtBaseController
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Kullanıcıyı ID ile getir
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        => ToActionResult(await _userService.GetByIdAsync(id, cancellationToken));

    /// <summary>
    /// Tüm kullanıcıları listele
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        => ToActionResult(await _userService.GetAllAsync(cancellationToken));

    /// <summary>
    /// Kullanıcıları sayfalı listele
    /// </summary>
    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
        => ToActionResult(await _userService.GetAllPagedAsync(pageNumber, pageSize, cancellationToken));

    /// <summary>
    /// Tüm kullanıcıları (admin) listele
    /// </summary>
    [HttpGet("admin")]
    public async Task<IActionResult> GetAllForAdmin(CancellationToken cancellationToken)
        => ToActionResult(await _userService.GetAllForAdminAsync(cancellationToken));

    /// <summary>
    /// Kullanıcıyı ID ile getir (admin)
    /// </summary>
    [HttpGet("admin/{id:int}")]
    public async Task<IActionResult> GetByIdForAdmin(int id, CancellationToken cancellationToken)
        => ToActionResult(await _userService.GetByIdForAdminAsync(id, cancellationToken));

    /// <summary>
    /// Kullanıcı adına göre kullanıcıyı getir
    /// </summary>
    [HttpGet("by-username/{username}")]
    public async Task<IActionResult> GetByUsername(string username, CancellationToken cancellationToken)
        => ToActionResult(await _userService.GetByUsernameAsync(username, cancellationToken));

    /// <summary>
    /// Yeni kullanıcı oluştur
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
        => ToActionResult(await _userService.CreateAsync(new CreateUserDto
        {
            Username = request.Username,
            Password = request.Password,
            CreatedBy = SortUserId()
        }, cancellationToken));

    /// <summary>
    /// Kullanıcı bilgilerini güncelle
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
        => ToActionResult(await _userService.UpdateAsync(new UpdateUserDto
        {
            Id = request.Id,
            Username = request.Username,
            Password = request.Password,
            UpdatedBy = SortUserId()
        }, cancellationToken));

    /// <summary>
    /// Kullanıcıyı sil
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        => ToActionResult(await _userService.DeleteAsync(id, cancellationToken));

    /// <summary>
    /// Kullanıcının aktiflik durumunu değiştir
    /// </summary>
    [HttpPatch("{id:int}/toggle-active")]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken cancellationToken)
        => ToActionResult(await _userService.ToggleActiveAsync(id, SortUserId(), cancellationToken));

    /// <summary>
    /// Silinen kullanıcıyı geri yükle
    /// </summary>
    [HttpPatch("{id:int}/restore")]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken)
        => ToActionResult(await _userService.RestoreAsync(id, SortUserId(), cancellationToken));
}
