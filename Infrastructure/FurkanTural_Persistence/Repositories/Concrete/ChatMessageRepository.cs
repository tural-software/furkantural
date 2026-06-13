using FurkanTural_Application.DTOs.ChatMessage;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Domain.Entities;
using FurkanTural_Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace FurkanTural_Persistence.Repositories.Concrete;

public class ChatMessageRepository(FurkanTuralDbContext context) : Repository<ChatMessage>(context), IChatMessageRepository
{
    public async Task<List<ConversationAggregateDto>> GetConversationAggregatesAsync(int userId, CancellationToken cancellationToken = default)
    {
        // 1) Karşı kullanıcı başına son mesaj zamanı + okunmamış sayısı — tek GROUP BY sorgusu.
        //    (Global query filter !IsDeleted && IsActive otomatik uygulanır.)
        var stats = await _dbSet.AsNoTracking()
            .Where(m => m.SenderId == userId || m.ReceiverId == userId)
            .GroupBy(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
            .Select(g => new
            {
                OtherUserId = g.Key,
                LastMessageAt = (DateTime?)g.Max(m => m.CreatedAt),
                UnreadCount = g.Count(m => m.ReceiverId == userId && !m.IsRead)
            })
            .ToListAsync(cancellationToken);

        if (stats.Count == 0)
            return [];

        // 2) Son mesajların içerik/tür bilgisi — zaman damgaları üzerinden tek sorgu.
        var lastTimes = stats.Where(s => s.LastMessageAt is not null)
                             .Select(s => s.LastMessageAt!.Value)
                             .ToList();

        var lastMessages = await _dbSet.AsNoTracking()
            .Where(m => (m.SenderId == userId || m.ReceiverId == userId) && lastTimes.Contains(m.CreatedAt))
            .Select(m => new { m.SenderId, m.ReceiverId, m.Content, m.MessageType, m.CreatedAt })
            .ToListAsync(cancellationToken);

        return stats.Select(s =>
        {
            var last = lastMessages.FirstOrDefault(m =>
                m.CreatedAt == s.LastMessageAt &&
                (m.SenderId == userId ? m.ReceiverId : m.SenderId) == s.OtherUserId);

            return new ConversationAggregateDto
            {
                OtherUserId = s.OtherUserId,
                LastMessage = last?.Content,
                LastMessageType = last?.MessageType,
                LastMessageAt = s.LastMessageAt,
                UnreadCount = s.UnreadCount
            };
        }).ToList();
    }
}
