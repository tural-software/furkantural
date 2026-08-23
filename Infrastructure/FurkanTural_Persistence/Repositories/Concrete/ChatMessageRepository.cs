using FurkanTural_Application.DTOs.ChatMessage;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Domain.Entities;
using FurkanTural_Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace FurkanTural_Persistence.Repositories.Concrete;

/// <summary>Toplu sonuç tek sorgudan değil ikiden gelir: önce karşı kullanıcı başına son mesaj zamanı ve okunmamış sayısı bir GROUP BY ile, ardından o zaman damgalarına düşen mesajların içerik ve türü ayrı bir sorguyla alınır. İkisi de EF üzerindedir, dolayısıyla canlı satır süzgeci kendiliğinden uygulanır.</summary>
public class ChatMessageRepository(FurkanTuralDbContext context) : Repository<ChatMessage>(context), IChatMessageRepository
{
    public async Task<List<ConversationAggregateDto>> GetConversationAggregatesAsync(int userId, CancellationToken cancellationToken = default)
    {
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
