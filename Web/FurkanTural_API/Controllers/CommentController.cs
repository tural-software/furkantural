using Asp.Versioning;
using FurkanTural_API.Controllers.Base;
using FurkanTural_API.Models.Comment;
using FurkanTural_API.Models.Common;
using FurkanTural_Application.DTOs.Comment;
using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.Services.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_API.Controllers;

/// <summary>Yorum uçları. Ziyaretçiye açık olan üç tanedir — bir yazının yayındaki yorumlarını okumak, yorum bırakmak ve yanıt bildirimlerini kapatmak — geri kalanı yönetime aittir.<para>Ziyaretçiye açık okuma adres taşımaz; adresi gören tek uç yönetim listesidir.</para></summary>
[ApiVersion("1.0")]
public class CommentController(ICommentService commentService) : JwtBaseController
{
    private readonly ICommentService _commentService = commentService;

    /// <summary>Bir yazının yayındaki yorumları; yanıtlar kök yorumların altında gelir. E-posta adresi döndürmez</summary>
    [HttpGet("blog/{blogId:int}")]
    [Authorize(Policy = "VisitorOrAbove")]
    public async Task<IActionResult> GetThread(int blogId, CancellationToken cancellationToken)
        => ToActionResult(await _commentService.GetThreadAsync(blogId, cancellationToken));

    /// <summary>Yorum bırak. Yorum beklemede kaydedilir ve onaydan önce görünmez; yanıt, kaydın gerçekten açılıp açılmadığını ele vermez</summary>
    [HttpPost]
    [Authorize(Policy = "VisitorOrAbove")]
    public async Task<IActionResult> Submit([FromBody] SubmitCommentRequest request, CancellationToken cancellationToken)
        => ToActionResult(await _commentService.SubmitAsync(
            new SubmitCommentDto
            {
                BlogId = request.BlogId,
                ParentId = request.ParentId,
                AuthorName = request.AuthorName,
                AuthorEmail = request.AuthorEmail,
                Body = request.Body,
                NotifyOnReply = request.NotifyOnReply
            },
            request.TurnstileToken, ClientIp(), cancellationToken));

    /// <summary>Yanıt bildirimlerini kapat. Jeton bildirim postasındaki bağlantıdan gelir ve o adresin bütün yorumlarında bildirimi kapatır</summary>
    [HttpPost("disable-notifications")]
    [Authorize(Policy = "VisitorOrAbove")]
    public async Task<IActionResult> DisableNotifications([FromBody] CommentTokenRequest request, CancellationToken cancellationToken)
        => ToActionResult(await _commentService.DisableNotificationsAsync(request.Token, cancellationToken));

    /// <summary>Yorumu ID ile getir (admin)</summary>
    [HttpGet("admin/{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetByIdForAdmin(int id, CancellationToken cancellationToken)
        => ToActionResult(await _commentService.GetByIdForAdminAsync(id, cancellationToken));

    /// <summary>Yönetici paneli için süzülmüş ve sayfalı yorum listesi</summary>
    [HttpGet("admin/paged")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAdminPaged(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] bool? isDeleted,
        [FromQuery] string? status,
        [FromQuery] int? blogId,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
        => ToActionResult(await _commentService.GetAllForAdminPagedAsync(
            AdminListQuery.From(search, isActive, isDeleted, dateFrom, dateTo, pageNumber, pageSize),
            status, blogId, cancellationToken));

    /// <summary>Yönetici paneli için yorum durum sayaçları; süzgeçler sayfalı listeyle aynıdır</summary>
    [HttpGet("admin/counts")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAdminCounts(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] bool? isDeleted,
        [FromQuery] string? status,
        [FromQuery] int? blogId,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        CancellationToken cancellationToken = default)
        => ToActionResult(await _commentService.GetAdminStatusCountsAsync(
            AdminListQuery.From(search, isActive, isDeleted, dateFrom, dateTo), status, blogId, cancellationToken));

    /// <summary>Yönetici paneli için yorum özetini getir (toplam + son işlem tarihi)</summary>
    [HttpGet("admin/summary")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAdminSummary(CancellationToken cancellationToken)
        => ToActionResult(await _commentService.GetAdminSummaryAsync(cancellationToken));

    /// <summary>Denetim kuyruğunun sayaçları: bekleyen, onaylanmış ve reddedilmiş yorum sayısı</summary>
    [HttpGet("admin/moderation-counts")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetModerationCounts(CancellationToken cancellationToken)
        => ToActionResult(await _commentService.GetModerationCountsAsync(cancellationToken));

    /// <summary>Yorumun denetim durumunu değiştir. Onaya geçen bir yanıt, üst yorumun sahibi istediyse bildirim kuyruğuna girer</summary>
    [HttpPatch("{id:int}/status")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> SetStatus(int id, [FromBody] CommentStatusRequest request, CancellationToken cancellationToken)
        => ToActionResult(await _commentService.SetStatusAsync(id, request.Status, SortUserId(), cancellationToken));

    /// <summary>Yoruma yazının sahibi olarak yanıt ver. Yanıt beklemeden yayına girer</summary>
    [HttpPost("{id:int}/reply")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Reply(int id, [FromBody] CommentReplyRequest request, CancellationToken cancellationToken)
        => ToActionResult(await _commentService.ReplyAsync(
            new AdminReplyCommentDto { ParentId = id, Body = request.Body }, SortUserId(), cancellationToken));

    /// <summary>Yorumu sil</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        => ToActionResult(await _commentService.DeleteAsync(id, SortUserId(), cancellationToken));

    /// <summary>Yorumun aktiflik durumunu değiştir</summary>
    [HttpPatch("{id:int}/toggle-active")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken cancellationToken)
        => ToActionResult(await _commentService.ToggleActiveAsync(id, SortUserId(), cancellationToken));

    /// <summary>Silinen yorumu geri yükle</summary>
    [HttpPatch("{id:int}/restore")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken)
        => ToActionResult(await _commentService.RestoreAsync(id, SortUserId(), cancellationToken));

    /// <summary>Seçili kayıtlara tek istekte uygulanır: siler, geri yükler, aktife ya da pasife alır. Uygun durumda olmayan kayıtlar atlanır ve yanıtta listelenir; en çok 100 kimlik</summary>
    [HttpPost("admin/bulk")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Bulk([FromBody] BulkActionRequest request, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<BulkAction>(request.Action, ignoreCase: true, out var action))
            return BadRequest(new { success = false, statusCode = 400, errors = new[] { "Geçersiz toplu işlem türü." } });

        return ToActionResult(await _commentService.BulkAsync(action, request.Ids ?? [], SortUserId(), cancellationToken));
    }

    private string? ClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
