namespace FurkanTural_Application.Services.Abstract;

/// <summary>
/// Kullanıcıların çevrim içi durumu. Kullanıcı başına bağlantılar sayıldığından aynı kişinin birden
/// çok sekmesi veya cihazı birbirini düşürmez. Connect ile Disconnect'in döndürdüğü bool "işlem
/// başarılı" demek değildir: Connect yalnızca kullanıcının ilk bağlantısında, Disconnect yalnızca son
/// bağlantısı da kapandığında true döner — durum değişimi bildirimi buna bakılarak yayınlanır.
/// Bellek içinde tutulur; süreç yeniden başlarsa herkes çevrim dışı sayılır.
/// </summary>
public interface IPresenceTracker
{
    bool Connect(int userId, string connectionId);
    bool Disconnect(int userId, string connectionId);
    bool IsOnline(int userId);
}