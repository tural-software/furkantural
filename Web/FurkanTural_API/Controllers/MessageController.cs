using Asp.Versioning;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_API.Controllers.Base;
using FurkanTural_API.Models.Message;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_API.Controllers;

[Authorize(Policy = "UserOrAdmin")]
[ApiVersion("1.0")]
public class MessageController(IChatMessageService chatMessageService, IChatNotifier chatNotifier, IFileService fileService) : JwtBaseController
{
    private readonly IChatMessageService _chatMessageService = chatMessageService;
    private readonly IChatNotifier _chatNotifier = chatNotifier;
    private readonly IFileService _fileService = fileService;

    /// <summary>Mesaj gönder (REST; canlı dağıtım SignalR Hub üzerinden de yapılır)</summary>
    [HttpPost]
    public async Task<IActionResult> Send([FromBody] SendMessageRequest request, CancellationToken cancellationToken)
    {
        var userId = SortUserId();
        if (userId is null) return Unauthorized();

        var result = await _chatMessageService.SendAsync(userId.Value, request.ReceiverId, request.Content, cancellationToken);
        if (result.Success && result.Data is not null)
            await _chatNotifier.NotifyMessageReceivedAsync(request.ReceiverId, result.Data);

        return ToActionResult(result);
    }

    /// <summary>Sesli mesaj (voice note) gönder</summary>
    [HttpPost("audio")]
    public async Task<IActionResult> SendAudio([FromBody] SendAudioRequest request, CancellationToken cancellationToken)
    {
        var userId = SortUserId();
        if (userId is null) return Unauthorized();
        if (request.AudioData is null || request.AudioData.Length == 0)
            return BadRequest("Ses verisi boş olamaz.");

        var fileName = await _fileService.SaveAsync(request.AudioData, request.AudioName ?? "voice.webm", "ChatMessage", request.ReceiverId, userId.Value);

        var result = await _chatMessageService.SendAudioAsync(userId.Value, request.ReceiverId, fileName, request.DurationSeconds, cancellationToken);
        if (result.Success && result.Data is not null)
            await _chatNotifier.NotifyMessageReceivedAsync(request.ReceiverId, result.Data);
        else
            await _fileService.DeleteAsync(fileName); // başarısızsa yetim dosyayı temizle

        return ToActionResult(result);
    }

    /// <summary>Foto/Video mesajı gönder (foto ≤10MB, video ≤30MB)</summary>
    [HttpPost("media")]
    [RequestSizeLimit(48_000_000)] // base64(30MB) ≈ 40MB + JSON payı; gerçek sınır FileService maxBytes
    public async Task<IActionResult> SendMedia([FromBody] SendMediaRequest request, CancellationToken cancellationToken)
    {
        var userId = SortUserId();
        if (userId is null) return Unauthorized();
        if (request.Data is null || request.Data.Length == 0)
            return BadRequest("Medya verisi boş olamaz.");

        var isVideo = string.Equals(request.MediaType, "Video", StringComparison.OrdinalIgnoreCase);
        var maxBytes = isVideo ? 30L * 1024 * 1024 : 10L * 1024 * 1024;

        string fileName;
        try
        {
            fileName = await _fileService.SaveAsync(request.Data, request.FileName, "ChatMessage", request.ReceiverId, userId.Value, maxBytes);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message); // desteklenmeyen uzantı veya boyut aşımı
        }

        var result = await _chatMessageService.SendMediaAsync(userId.Value, request.ReceiverId, fileName, isVideo ? "Video" : "Image", request.DurationSeconds, cancellationToken);
        if (result.Success && result.Data is not null)
            await _chatNotifier.NotifyMessageReceivedAsync(request.ReceiverId, result.Data);
        else
            await _fileService.DeleteAsync(fileName); // başarısızsa yetim dosyayı temizle

        return ToActionResult(result);
    }

    /// <summary>Bir arkadaşla olan konuşmayı getir</summary>
    [HttpGet("conversation/{otherUserId:int}")]
    public async Task<IActionResult> GetConversation(int otherUserId, [FromQuery] int? take, CancellationToken cancellationToken)
    {
        var userId = SortUserId();
        if (userId is null) return Unauthorized();
        return ToActionResult(await _chatMessageService.GetConversationAsync(userId.Value, otherUserId, take, cancellationToken));
    }

    /// <summary>Tüm konuşmaların özetini getir (son mesaj + okunmamış sayısı)</summary>
    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations(CancellationToken cancellationToken)
    {
        var userId = SortUserId();
        if (userId is null) return Unauthorized();
        return ToActionResult(await _chatMessageService.GetConversationsAsync(userId.Value, cancellationToken));
    }

    /// <summary>Bir arkadaşla olan konuşmayı okundu işaretle</summary>
    [HttpPost("{otherUserId:int}/read")]
    public async Task<IActionResult> MarkRead(int otherUserId, CancellationToken cancellationToken)
    {
        var userId = SortUserId();
        if (userId is null) return Unauthorized();

        var result = await _chatMessageService.MarkConversationReadAsync(userId.Value, otherUserId, cancellationToken);
        if (result.Success)
            await _chatNotifier.NotifyMessageReadAsync(otherUserId, userId.Value);

        return ToActionResult(result);
    }

    // ── Admin ──

    /// <summary>Tüm mesajları (admin) sayfalı listele</summary>
    [HttpGet("admin")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAllForAdmin([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        => ToActionResult(await _chatMessageService.GetAllPagedForAdminAsync(pageNumber, pageSize, cancellationToken));

    /// <summary>Mesajı ID ile getir (admin)</summary>
    [HttpGet("admin/{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetByIdForAdmin(int id, CancellationToken cancellationToken)
        => ToActionResult(await _chatMessageService.GetByIdForAdminAsync(id, cancellationToken));

    /// <summary>Mesajın aktiflik durumunu değiştir</summary>
    [HttpPatch("{id:int}/toggle-active")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken cancellationToken)
        => ToActionResult(await _chatMessageService.ToggleActiveAsync(id, SortUserId(), cancellationToken));

    /// <summary>Silinen mesajı geri yükle</summary>
    [HttpPatch("{id:int}/restore")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken)
        => ToActionResult(await _chatMessageService.RestoreAsync(id, SortUserId(), cancellationToken));

    /// <summary>Yönetici paneli için mesaj özetini getir</summary>
    [HttpGet("admin/summary")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAdminSummary(CancellationToken cancellationToken)
        => ToActionResult(await _chatMessageService.GetAdminSummaryAsync(cancellationToken));
}
