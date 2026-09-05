using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.DTOs.User;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_API.Controllers.Base;
using FurkanTural_API.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace FurkanTural_API.Controllers;

[ApiVersion("1.0")]
public class UserController : JwtBaseController
{
    private readonly IUserService _userService;
    private readonly IFileService _fileService;

    public UserController(IUserService userService, IFileService fileService)
    {
        _userService = userService;
        _fileService = fileService;
    }

    /// <summary>Kullanıcıyı ID ile getir</summary>
    [HttpGet("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        => ToActionResult(await _userService.GetByIdAsync(id, cancellationToken));

    /// <summary>Tüm kullanıcıları listele</summary>
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        => ToActionResult(await _userService.GetAllAsync(cancellationToken));

    /// <summary>Kullanıcıları sayfalı listele</summary>
    [HttpGet("paged")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
        => ToActionResult(await _userService.GetAllPagedAsync(pageNumber, pageSize, cancellationToken));

    /// <summary>Kullanıcı ara (üyeler arkadaş eklemek için kullanır)</summary>
    [HttpGet("search")]
    [Authorize(Policy = "UserOrAdmin")]
    public async Task<IActionResult> Search([FromQuery] string query, CancellationToken cancellationToken)
    {
        var userId = SortUserId();
        if (userId is null) return Unauthorized();
        return ToActionResult(await _userService.SearchAsync(query ?? string.Empty, userId.Value, cancellationToken));
    }

    /// <summary>Tüm kullanıcıları (admin) listele</summary>
    [HttpGet("admin")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAllForAdmin(CancellationToken cancellationToken)
        => ToActionResult(await _userService.GetAllForAdminAsync(cancellationToken));

    /// <summary>Kullanıcıyı ID ile getir (admin)</summary>
    [HttpGet("admin/{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetByIdForAdmin(int id, CancellationToken cancellationToken)
        => ToActionResult(await _userService.GetByIdForAdminAsync(id, cancellationToken));

    /// <summary>Kullanıcı adına göre kullanıcıyı getir</summary>
    [HttpGet("by-username/{username}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetByUsername(string username, CancellationToken cancellationToken)
        => ToActionResult(await _userService.GetByUsernameAsync(username, cancellationToken));

    /// <summary>Yeni kullanıcı oluştur</summary>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
        => ToActionResult(await _userService.CreateAsync(new CreateUserDto
        {
            Username = request.Username,
            Password = request.Password,
            RoleId = request.RoleId,
            Email = request.Email,
            DisplayName = request.DisplayName,
            AvatarUrl = request.AvatarUrl,
            CreatedBy = SortUserId()
        }, cancellationToken));

    /// <summary>Kullanıcı bilgilerini güncelle</summary>
    [HttpPut]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Update([FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
        => ToActionResult(await _userService.UpdateAsync(new UpdateUserDto
        {
            Id = request.Id,
            Username = request.Username,
            Password = request.Password,
            RoleId = request.RoleId,
            Email = request.Email,
            DisplayName = request.DisplayName,
            AvatarUrl = request.AvatarUrl,
            UpdatedBy = SortUserId()
        }, cancellationToken));

    /// <summary>Kullanıcıyı sil</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        => ToActionResult(await _userService.DeleteAsync(id, SortUserId(), cancellationToken));

    /// <summary>Kullanıcının aktiflik durumunu değiştir</summary>
    [HttpPatch("{id:int}/toggle-active")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken cancellationToken)
        => ToActionResult(await _userService.ToggleActiveAsync(id, SortUserId(), cancellationToken));

    /// <summary>Silinen kullanıcıyı geri yükle</summary>
    [HttpPatch("{id:int}/restore")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken)
        => ToActionResult(await _userService.RestoreAsync(id, SortUserId(), cancellationToken));

    /// <summary>Yönetici paneli için kullanıcı özetini getir (toplam + son işlem tarihi)</summary>
    [HttpGet("admin/summary")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAdminSummary(CancellationToken cancellationToken)
        => ToActionResult(await _userService.GetAdminSummaryAsync(cancellationToken));

    /// <summary>Yönetici paneli için süzülmüş ve sayfalı kullanıcı listesi; seenSince verilirse yalnızca o andan sonra görülen kullanıcılar gelir</summary>
    [HttpGet("admin/paged")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAdminPaged(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] bool? isDeleted,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int? roleId,
        [FromQuery] DateTime? seenSince,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
        => ToActionResult(await _userService.GetAllForAdminPagedAsync(
            AdminListQuery.From(search, isActive, isDeleted, dateFrom, dateTo, pageNumber, pageSize), roleId, seenSince, cancellationToken));

    /// <summary>Yönetici paneli için kullanıcı durum sayaçları; süzgeçler sayfalı listeyle aynıdır</summary>
    [HttpGet("admin/counts")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAdminCounts(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] bool? isDeleted,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int? roleId,
        [FromQuery] DateTime? seenSince,
        CancellationToken cancellationToken = default)
        => ToActionResult(await _userService.GetAdminStatusCountsAsync(
            AdminListQuery.From(search, isActive, isDeleted, dateFrom, dateTo), roleId, seenSince, cancellationToken));

    /// <summary>Bir kullanıcının avatar fotoğrafını yükle (admin)</summary>
    [HttpPost("{id:int}/avatar")]
    [Authorize(Policy = "AdminOnly")]
    public Task<IActionResult> UploadAvatar(int id, [FromBody] UserAvatarRequest request, CancellationToken cancellationToken)
        => SaveAvatarAsync(id, request, cancellationToken);

    /// <summary>Giriş yapan kullanıcının kendi avatarını yükle</summary>
    [HttpPost("me/avatar")]
    [Authorize(Policy = "UserOrAdmin")]
    public async Task<IActionResult> UploadMyAvatar([FromBody] UserAvatarRequest request, CancellationToken cancellationToken)
    {
        var userId = SortUserId();
        if (userId is null) return Unauthorized();
        return await SaveAvatarAsync(userId.Value, request, cancellationToken);
    }

    /// <summary>Giriş yapan kullanıcı üyelik sözleşmesini kabul eder (eski üyeler için tek seferlik onay).</summary>
    [HttpPost("me/accept-agreement")]
    [Authorize(Policy = "UserOrAdmin")]
    public async Task<IActionResult> AcceptAgreement(CancellationToken cancellationToken)
    {
        var userId = SortUserId();
        if (userId is null) return Unauthorized();
        return ToActionResult(await _userService.AcceptAgreementAsync(userId.Value, cancellationToken));
    }


    /// <summary>Giriş yapan kullanıcı kendi hesabını kapatır; geri açmak e-posta doğrulaması ister</summary>
    [HttpPost("me/deactivate")]
    [Authorize(Policy = "UserOrAdmin")]
    public async Task<IActionResult> DeactivateMyAccount([FromBody] DeactivateAccountRequest request, CancellationToken cancellationToken)
    {
        var userId = SortUserId();
        if (userId is null) return Unauthorized();
        return ToActionResult(await _userService.DeactivateMyAccountAsync(userId.Value, request.Password, cancellationToken));
    }

    private const long AvatarMaxBytes = 5L * 1024 * 1024;

    private async Task<IActionResult> SaveAvatarAsync(int targetUserId, UserAvatarRequest request, CancellationToken cancellationToken)
    {
        if (request.ImageData is null || request.ImageData.Length == 0)
            return BadRequest("Avatar verisi boş olamaz.");

        var actingUserId = SortUserId() ?? 0;
        string fileName;
        try
        {
            fileName = await _fileService.SaveAsync(request.ImageData, request.ImageName ?? "avatar.png", "User", targetUserId, actingUserId, AvatarMaxBytes);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }

        return ToActionResult(await _userService.UpdateAvatarAsync(targetUserId, fileName, SortUserId(), cancellationToken));
    }

    /// <summary>Sistemde hiç kullanıcı yoksa ilk admin kullanıcısını oluştur</summary>
    [AllowAnonymous]
    [HttpPost("seed-admin")]
    public async Task<IActionResult> SeedAdmin([FromBody] SeedAdminRequest request, CancellationToken cancellationToken)
        => ToActionResult(await _userService.SeedAdminAsync(request.Username, request.Password, cancellationToken));
}
