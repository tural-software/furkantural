using FurkanTural_Domain.Entities;

namespace FurkanTural_Business.Helpers;

/// <summary>Hesabın kapanışını ve yeniden açılışını tek yerden işaretler. Yönetici kapatması ile kullanıcının kendi kapatması ayrı tutulur: kullanıcı kendi kapattığı hesabı postadaki bağlantıyla geri açabilir, yöneticinin koyduğu yasak ise yalnızca yönetici tarafından kaldırılır.<para>Kapanış anı saklama süresinin başladığı andır; aydınlatma metni hesap verilerini "kapatıldıktan sonra 2 yıl" saklamayı söyler ve bu an başka hiçbir sütundan güvenilir biçimde okunamaz.</para></summary>
public static class AccountClosure
{
    public static void Close(User user, DateTime now, bool byAdmin)
    {
        user.IsActive = false;
        user.DeactivatedAt = now;
        user.DeactivatedByAdmin = byAdmin;
    }

    public static void Reopen(User user)
    {
        user.IsActive = true;
        user.DeactivatedAt = null;
        user.DeactivatedByAdmin = false;
    }
}
