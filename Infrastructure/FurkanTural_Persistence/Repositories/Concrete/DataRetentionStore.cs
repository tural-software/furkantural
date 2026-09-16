using FurkanTural_Application.DTOs.Retention;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Domain.Constants;
using FurkanTural_Domain.Entities;
using FurkanTural_Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace FurkanTural_Persistence.Repositories.Concrete;

/// <summary>Saklama kuralları. Süre aydınlatma metinlerinin söylediği olaydan işler: kayıt ve iz verileri oluşturulma anından, silinen ya da reddedilen kayıtlar silinme/ret anından, kapatılmış hesaplar kapatılma anından. Sorguların hepsi global süzgeci atlar; süzgeç silinmiş ve pasif satırları gizler, oysa silinmesi gereken tam olarak onlardır.<para>Şemada ayrı bir "ret" ya da "sonuçlanma" sütunu olmayan kayıtlarda (reddedilmiş yorum, sonuçlanmış şikâyet) son güncelleme anı kullanılır. Sonraki bir güncelleme bu anı ileri taşır; sonuç kaydın daha geç silinmesidir, hiçbir zaman erken silinmesi değil.</para><para>Süren bir aboneliğin doğrulama kayıtları silinmez: izinli pazarlamanın kanıtıdırlar ve metin onları abonelik sürdükçe saklamayı söyler. Yönetici hesapları kapalı olsa bile silinmez; panelin kendi erişimini kaybetmesi geri alınamaz bir hatadır.</para></summary>
public static class RetentionQueries
{
    private static readonly string[] ResolvedReportStatuses =
        [ReportDefinitions.Statuses.Reviewed, ReportDefinitions.Statuses.Dismissed, ReportDefinitions.Statuses.ActionTaken];

    private static IQueryable<T> All<T>(FurkanTuralDbContext db) where T : class => db.Set<T>().IgnoreQueryFilters();

    public static IQueryable<User> ClosedUsers(FurkanTuralDbContext db, DateTime cutoff)
    {
        var adminRoles = All<Role>(db).Where(r => r.Name == "Admin").Select(r => r.Id);
        return All<User>(db).Where(u => !adminRoles.Contains(u.RoleId)
            && ((u.IsDeleted && u.DeletedAt != null && u.DeletedAt < cutoff)
                || (!u.IsDeleted && !u.IsActive && u.DeactivatedAt != null && u.DeactivatedAt < cutoff)));
    }

    public static IQueryable<Log> Logs(FurkanTuralDbContext db, DateTime cutoff)
        => All<Log>(db).Where(l => l.Date < cutoff);

    public static IQueryable<Contact> Contacts(FurkanTuralDbContext db, DateTime cutoff)
        => All<Contact>(db).Where(c => c.CreatedAt < cutoff);

    public static IQueryable<AccountActivation> AccountActivations(FurkanTuralDbContext db, DateTime cutoff)
    {
        var closed = ClosedUsers(db, cutoff).Select(u => u.Id);
        return All<AccountActivation>(db).Where(a => a.CreatedAt < cutoff || closed.Contains(a.UserId));
    }

    public static IQueryable<CallLog> CallLogs(FurkanTuralDbContext db, DateTime cutoff)
    {
        var closed = ClosedUsers(db, cutoff).Select(u => u.Id);
        return All<CallLog>(db).Where(c => c.StartedAt < cutoff || closed.Contains(c.CallerId) || closed.Contains(c.CalleeId));
    }

    public static IQueryable<ChatMessage> ChatMessages(FurkanTuralDbContext db, DateTime cutoff)
    {
        var closed = ClosedUsers(db, cutoff).Select(u => u.Id);
        return All<ChatMessage>(db).Where(m => (m.IsDeleted && m.DeletedAt != null && m.DeletedAt < cutoff)
            || closed.Contains(m.SenderId) || closed.Contains(m.ReceiverId));
    }

    public static IQueryable<Report> Reports(FurkanTuralDbContext db, DateTime cutoff)
    {
        var closed = ClosedUsers(db, cutoff).Select(u => u.Id);
        return All<Report>(db).Where(r =>
            (r.Status != null && ResolvedReportStatuses.Contains(r.Status) && (r.UpdatedAt ?? r.CreatedAt) < cutoff)
            || closed.Contains(r.ReporterId)
            || (r.ReportedUserId != null && closed.Contains(r.ReportedUserId.Value)));
    }

    public static IQueryable<UserFriend> UserFriends(FurkanTuralDbContext db, DateTime cutoff)
    {
        var closed = ClosedUsers(db, cutoff).Select(u => u.Id);
        return All<UserFriend>(db).Where(f => closed.Contains(f.RequesterId) || closed.Contains(f.AddresseeId));
    }

    public static IQueryable<Subscriber> Subscribers(FurkanTuralDbContext db, DateTime cutoff)
        => All<Subscriber>(db).Where(s => (s.IsDeleted && s.DeletedAt != null && s.DeletedAt < cutoff)
            || (!s.IsDeleted && s.ConfirmedAt == null && s.CreatedAt < cutoff));

    public static IQueryable<SubscriberVerification> SubscriberVerifications(FurkanTuralDbContext db, DateTime cutoff)
    {
        var expired = Subscribers(db, cutoff).Select(s => s.Id);
        return All<SubscriberVerification>(db).Where(v => expired.Contains(v.SubscriberId));
    }

    public static IQueryable<NewsletterDelivery> NewsletterDeliveries(FurkanTuralDbContext db, DateTime cutoff)
    {
        var expired = Subscribers(db, cutoff).Select(s => s.Id);
        return All<NewsletterDelivery>(db).Where(d => (d.SentAt ?? d.CreatedAt) < cutoff || expired.Contains(d.SubscriberId));
    }

    public static IQueryable<Comment> CommentCandidates(FurkanTuralDbContext db, DateTime cutoff)
        => All<Comment>(db).Where(c => (c.IsDeleted && c.DeletedAt != null && c.DeletedAt < cutoff)
            || (c.Status == CommentStatuses.Rejected && (c.UpdatedAt ?? c.CreatedAt) < cutoff));

    public static IQueryable<CommentNotification> CommentNotifications(FurkanTuralDbContext db, DateTime cutoff, IReadOnlyCollection<int> commentIds)
        => All<CommentNotification>(db).Where(n => (n.SentAt ?? n.CreatedAt) < cutoff || commentIds.Contains(n.CommentId));

    public static IQueryable<PushSubscription> StalePushSubscriptions(FurkanTuralDbContext db, DateTime staleBefore)
        => All<PushSubscription>(db).Where(p => (p.UpdatedAt ?? p.CreatedAt) < staleBefore);
}

public class DataRetentionStore(FurkanTuralDbContext context) : IDataRetentionStore
{
    private readonly FurkanTuralDbContext _context = context;

    public async Task<IReadOnlyList<DataRetentionCategoryDto>> CountAsync(DateTime cutoff, DateTime staleBefore, CancellationToken cancellationToken = default)
    {
        var commentIds = await ExpiredCommentIdsAsync(cutoff, cancellationToken);

        return
        [
            new("logs", "İşlem ve hata kayıtları", await RetentionQueries.Logs(_context, cutoff).CountAsync(cancellationToken)),
            new("contacts", "İletişim formu mesajları", await RetentionQueries.Contacts(_context, cutoff).CountAsync(cancellationToken)),
            new("activations", "Hesap yeniden açma istekleri", await RetentionQueries.AccountActivations(_context, cutoff).CountAsync(cancellationToken)),
            new("calls", "Arama kayıtları", await RetentionQueries.CallLogs(_context, cutoff).CountAsync(cancellationToken)),
            new("messages", "Silinmiş mesajlar ve kapatılmış hesapların mesajları", await RetentionQueries.ChatMessages(_context, cutoff).CountAsync(cancellationToken)),
            new("reports", "Sonuçlanmış şikâyetler", await RetentionQueries.Reports(_context, cutoff).CountAsync(cancellationToken)),
            new("friendships", "Kapatılmış hesapların arkadaşlık ve engelleme kayıtları", await RetentionQueries.UserFriends(_context, cutoff).CountAsync(cancellationToken)),
            new("commentNotifications", "Yorum yanıtı bildirim kayıtları", await RetentionQueries.CommentNotifications(_context, cutoff, commentIds).CountAsync(cancellationToken)),
            new("comments", "Silinmiş ya da reddedilmiş yorumlar", commentIds.Count),
            new("newsletterDeliveries", "Bülten gönderim kayıtları", await RetentionQueries.NewsletterDeliveries(_context, cutoff).CountAsync(cancellationToken)),
            new("subscriberVerifications", "Çıkmış ya da doğrulanmamış abonelerin doğrulama kayıtları", await RetentionQueries.SubscriberVerifications(_context, cutoff).CountAsync(cancellationToken)),
            new("subscribers", "Çıkmış ya da doğrulanmamış bülten aboneleri", await RetentionQueries.Subscribers(_context, cutoff).CountAsync(cancellationToken)),
            new("pushSubscriptions", "30 gündür kullanılmayan bildirim abonelikleri", await RetentionQueries.StalePushSubscriptions(_context, staleBefore).CountAsync(cancellationToken)),
            new("users", "Kapatılmış hesaplar", await RetentionQueries.ClosedUsers(_context, cutoff).CountAsync(cancellationToken))
        ];
    }

    public async Task<DataRetentionPurge> PurgeAsync(DateTime cutoff, DateTime staleBefore, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var commentIds = await ExpiredCommentIdsAsync(cutoff, cancellationToken);

        var files = new List<string>();
        files.AddRange(await RetentionQueries.ChatMessages(_context, cutoff)
            .Where(m => m.AttachmentUrl != null && m.AttachmentUrl != "")
            .Select(m => m.AttachmentUrl!)
            .ToListAsync(cancellationToken));
        files.AddRange(await RetentionQueries.ClosedUsers(_context, cutoff)
            .Where(u => u.AvatarUrl != null && u.AvatarUrl != "")
            .Select(u => u.AvatarUrl!)
            .ToListAsync(cancellationToken));

        var commentNotifications = await RetentionQueries.CommentNotifications(_context, cutoff, commentIds).ExecuteDeleteAsync(cancellationToken);
        var comments = commentIds.Count == 0
            ? 0
            : await _context.Set<Comment>().IgnoreQueryFilters().Where(c => commentIds.Contains(c.Id)).ExecuteDeleteAsync(cancellationToken);
        var newsletterDeliveries = await RetentionQueries.NewsletterDeliveries(_context, cutoff).ExecuteDeleteAsync(cancellationToken);
        var subscriberVerifications = await RetentionQueries.SubscriberVerifications(_context, cutoff).ExecuteDeleteAsync(cancellationToken);
        var subscribers = await RetentionQueries.Subscribers(_context, cutoff).ExecuteDeleteAsync(cancellationToken);
        var messages = await RetentionQueries.ChatMessages(_context, cutoff).ExecuteDeleteAsync(cancellationToken);
        var calls = await RetentionQueries.CallLogs(_context, cutoff).ExecuteDeleteAsync(cancellationToken);
        var reports = await RetentionQueries.Reports(_context, cutoff).ExecuteDeleteAsync(cancellationToken);
        var friendships = await RetentionQueries.UserFriends(_context, cutoff).ExecuteDeleteAsync(cancellationToken);
        var activations = await RetentionQueries.AccountActivations(_context, cutoff).ExecuteDeleteAsync(cancellationToken);
        var pushSubscriptions = await RetentionQueries.StalePushSubscriptions(_context, staleBefore).ExecuteDeleteAsync(cancellationToken);
        var users = await RetentionQueries.ClosedUsers(_context, cutoff).ExecuteDeleteAsync(cancellationToken);
        var contacts = await RetentionQueries.Contacts(_context, cutoff).ExecuteDeleteAsync(cancellationToken);
        var logs = await RetentionQueries.Logs(_context, cutoff).ExecuteDeleteAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return new DataRetentionPurge(
        [
            new("logs", "İşlem ve hata kayıtları", logs),
            new("contacts", "İletişim formu mesajları", contacts),
            new("activations", "Hesap yeniden açma istekleri", activations),
            new("calls", "Arama kayıtları", calls),
            new("messages", "Silinmiş mesajlar ve kapatılmış hesapların mesajları", messages),
            new("reports", "Sonuçlanmış şikâyetler", reports),
            new("friendships", "Kapatılmış hesapların arkadaşlık ve engelleme kayıtları", friendships),
            new("commentNotifications", "Yorum yanıtı bildirim kayıtları", commentNotifications),
            new("comments", "Silinmiş ya da reddedilmiş yorumlar", comments),
            new("newsletterDeliveries", "Bülten gönderim kayıtları", newsletterDeliveries),
            new("subscriberVerifications", "Çıkmış ya da doğrulanmamış abonelerin doğrulama kayıtları", subscriberVerifications),
            new("subscribers", "Çıkmış ya da doğrulanmamış bülten aboneleri", subscribers),
            new("pushSubscriptions", "30 gündür kullanılmayan bildirim abonelikleri", pushSubscriptions),
            new("users", "Kapatılmış hesaplar", users)
        ], files);
    }

    private async Task<IReadOnlyCollection<int>> ExpiredCommentIdsAsync(DateTime cutoff, CancellationToken cancellationToken)
    {
        var candidates = (await RetentionQueries.CommentCandidates(_context, cutoff).Select(c => c.Id).ToListAsync(cancellationToken)).ToHashSet();
        if (candidates.Count == 0)
            return candidates;

        var children = await _context.Set<Comment>().IgnoreQueryFilters()
            .Where(c => c.ParentId != null && candidates.Contains(c.ParentId.Value))
            .Select(c => new { c.Id, ParentId = c.ParentId!.Value })
            .ToListAsync(cancellationToken);

        bool removed;
        do
        {
            var blocked = children.Where(c => candidates.Contains(c.ParentId) && !candidates.Contains(c.Id))
                .Select(c => c.ParentId).ToHashSet();
            removed = candidates.RemoveWhere(blocked.Contains) > 0;
        } while (removed);

        return candidates;
    }
}
