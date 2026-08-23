using FurkanTural_Application.DTOs.ChatMessage;
using FurkanTural_Domain.Entities;

namespace FurkanTural_Application.Repositories.Abstract;

/// <summary>Sohbet listesi ekranının toplu sorgusu. Karşı kullanıcı başına son mesaj ile okunmamış sayısını çıkarır; okunmamış yalnızca çağıranın alıcı olduğu mesajlardan sayılır. Karşı taraf ayrı bir sohbet kaydından değil, mesajın gönderen/alıcı alanlarından türetilir. Son mesajın içeriği zaman damgası üzerinden eşleştirilir, dolayısıyla aynı sohbette birebir aynı ana düşen iki mesaj varsa hangisinin döneceği belirsizdir.</summary>
public interface IChatMessageRepository : IRepository<ChatMessage>
{
    Task<List<ConversationAggregateDto>> GetConversationAggregatesAsync(int userId, CancellationToken cancellationToken = default);
}
