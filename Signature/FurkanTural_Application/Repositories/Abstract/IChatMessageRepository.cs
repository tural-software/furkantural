using FurkanTural_Application.DTOs.ChatMessage;
using FurkanTural_Domain.Entities;

namespace FurkanTural_Application.Repositories.Abstract;

/// <summary>
/// ChatMessage'a özgü sorgular. Genel CRUD <see cref="IRepository{T}"/>'den gelir;
/// burası tüm mesajları belleğe çekmeden veritabanında toplulaştırılan sorguları barındırır.
/// </summary>
public interface IChatMessageRepository : IRepository<ChatMessage>
{
    /// <summary>
    /// Kullanıcının taraf olduğu tüm konuşmalar için karşı kullanıcı başına
    /// son mesaj + okunmamış sayısını döndürür (N+1 yok, sabit sayıda sorgu).
    /// </summary>
    Task<List<ConversationAggregateDto>> GetConversationAggregatesAsync(int userId, CancellationToken cancellationToken = default);
}
