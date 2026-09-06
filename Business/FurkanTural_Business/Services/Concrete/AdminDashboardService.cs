using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using FurkanTural_Domain.Entities.Common;
using Microsoft.Extensions.Logging;

namespace FurkanTural_Business.Services.Concrete;

public class AdminDashboardService(IUnitOfWork unitOfWork, ILogger<AdminDashboardService> logger) : IAdminDashboardService
{
    public const int MinWindow = 1;
    public const int MaxWindow = 90;
    public const string PendingStatus = "Pending";

    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger<AdminDashboardService> _logger = logger;

    public async Task<Result<AdminDashboardDto>> GetAsync(DateTime today, int windowDays, CancellationToken cancellationToken = default)
    {
        var window = Math.Clamp(windowDays, MinWindow, MaxWindow);
        var day = today.Date;
        var thisFrom = day.AddDays(-(window - 1));
        var lastFrom = day.AddDays(-(2 * window - 1));

        var summaries = new Dictionary<string, EntitySummaryDto>(21);
        foreach (var (key, read) in Summaries())
        {
            try
            {
                summaries[key] = await read(cancellationToken);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException && cancellationToken.IsCancellationRequested))
            {
                _logger.LogWarning(ex, "Pano özeti okunamadı, kart boş bırakıldı: {Key}", key);
            }
        }

        var unread = await TryCountAsync(() => _unitOfWork.Contacts.CountForAdminAsync(x => !x.IsDeleted && !x.IsRead, cancellationToken), "okunmamış mesaj", cancellationToken);
        var pending = await TryCountAsync(() => _unitOfWork.Reports.CountForAdminAsync(x => !x.IsDeleted && x.Status == PendingStatus, cancellationToken), "bekleyen şikayet", cancellationToken);
        var active = await TryCountAsync(() => _unitOfWork.Users.CountForAdminAsync(x => !x.IsDeleted && x.IsActive && x.LastSeenAt != null && x.LastSeenAt >= thisFrom, cancellationToken), "aktif kullanıcı", cancellationToken);

        var thisWeek = await WeekAsync("bu hafta", thisFrom, day.AddDays(1), cancellationToken);
        var lastWeek = await WeekAsync("geçen hafta", lastFrom, thisFrom, cancellationToken);

        return Result<AdminDashboardDto>.Ok(new AdminDashboardDto(summaries, unread, pending, active, thisWeek, lastWeek));
    }

    private async Task<AdminWeeklyCountsDto> WeekAsync(string window, DateTime from, DateTime toExclusive, CancellationToken cancellationToken)
        => new(
            await TryCountAsync(() => _unitOfWork.Blogs.CountForAdminAsync(Created<FurkanTural_Domain.Entities.Blog>(from, toExclusive), cancellationToken), $"{window}/yazı", cancellationToken),
            await TryCountAsync(() => _unitOfWork.Users.CountForAdminAsync(Created<FurkanTural_Domain.Entities.User>(from, toExclusive), cancellationToken), $"{window}/kullanıcı", cancellationToken),
            await TryCountAsync(() => _unitOfWork.Contacts.CountForAdminAsync(Created<FurkanTural_Domain.Entities.Contact>(from, toExclusive), cancellationToken), $"{window}/mesaj", cancellationToken),
            await TryCountAsync(() => _unitOfWork.Subscribers.CountForAdminAsync(Created<FurkanTural_Domain.Entities.Subscriber>(from, toExclusive), cancellationToken), $"{window}/abone", cancellationToken));

    private async Task<int?> TryCountAsync(Func<Task<int>> read, string part, CancellationToken cancellationToken)
    {
        try
        {
            return await read();
        }
        catch (Exception ex) when (!(ex is OperationCanceledException && cancellationToken.IsCancellationRequested))
        {
            _logger.LogWarning(ex, "Pano sayacı okunamadı, boş bırakıldı: {Part}", part);
            return null;
        }
    }

    private static System.Linq.Expressions.Expression<Func<T, bool>> Created<T>(DateTime from, DateTime toExclusive) where T : BaseEntity
        => x => !x.IsDeleted && x.CreatedAt >= from && x.CreatedAt < toExclusive;

    private IEnumerable<(string Key, Func<CancellationToken, Task<EntitySummaryDto>> Read)> Summaries()
    {
        yield return ("blog", ct => _unitOfWork.Blogs.GetAdminSummaryAsync(ct));
        yield return ("blogimage", ct => _unitOfWork.BlogImages.GetAdminSummaryAsync(ct));
        yield return ("category", ct => _unitOfWork.Categories.GetAdminSummaryAsync(ct));
        yield return ("project", ct => _unitOfWork.Projects.GetAdminSummaryAsync(ct));
        yield return ("projectimage", ct => _unitOfWork.ProjectImages.GetAdminSummaryAsync(ct));
        yield return ("music", ct => _unitOfWork.Musics.GetAdminSummaryAsync(ct));
        yield return ("musicimage", ct => _unitOfWork.MusicImages.GetAdminSummaryAsync(ct));
        yield return ("skill", ct => _unitOfWork.Skills.GetAdminSummaryAsync(ct));
        yield return ("experience", ct => _unitOfWork.Experiences.GetAdminSummaryAsync(ct));
        yield return ("education", ct => _unitOfWork.Educations.GetAdminSummaryAsync(ct));
        yield return ("user", ct => _unitOfWork.Users.GetAdminSummaryAsync(ct));
        yield return ("friend", ct => _unitOfWork.UserFriends.GetAdminSummaryAsync(ct));
        yield return ("message", ct => _unitOfWork.ChatMessages.GetAdminSummaryAsync(ct));
        yield return ("call", ct => _unitOfWork.CallLogs.GetAdminSummaryAsync(ct));
        yield return ("report", ct => _unitOfWork.Reports.GetAdminSummaryAsync(ct));
        yield return ("contact", ct => _unitOfWork.Contacts.GetAdminSummaryAsync(ct));
        yield return ("mailtemplate", ct => _unitOfWork.MailTemplates.GetAdminSummaryAsync(ct));
        yield return ("subscriber", ct => _unitOfWork.Subscribers.GetAdminSummaryAsync(ct));
        yield return ("role", ct => _unitOfWork.Roles.GetAdminSummaryAsync(ct));
        yield return ("status", ct => _unitOfWork.Statuses.GetAdminSummaryAsync(ct));
        yield return ("log", ct => _unitOfWork.Logs.GetAdminSummaryAsync(ct));
    }
}
