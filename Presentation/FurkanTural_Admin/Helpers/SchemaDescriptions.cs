namespace FurkanTural_Admin.Helpers;

/// <summary>Şema sayfasındaki alan açıklamaları. Bilinçli olarak API'den gelmez: tip, uzunluk ve varsayılan EF modelinden okunur ve sapamaz, açıklama ise insan metnidir — eskimesi yanıltıcı değil, yalnızca eksiktir.<para>Arama önce <c>Entity.Alan</c> anahtarına, bulunamazsa yalın alan adına bakar; böylece her entity'de tekrar eden denetim alanları bir kez yazılır.</para></summary>
public static class SchemaDescriptions
{
    private static readonly Dictionary<string, string> Ortak = new(StringComparer.Ordinal)
    {
        ["Id"] = "Benzersiz kayıt tanımlayıcısı",
        ["CreatedAt"] = "Kaydın oluşturulma tarihi (UTC)",
        ["CreatedBy"] = "Kaydı oluşturan kullanıcı kimliği",
        ["UpdatedAt"] = "Son güncelleme tarihi (UTC)",
        ["UpdatedBy"] = "Kaydı son güncelleyen kullanıcı kimliği",
        ["IsActive"] = "Aktiflik durumu — pasif kayıtlar genel sorgulara girmez",
        ["IsDeleted"] = "Yumuşak silme bayrağı — satır fiziksel olarak durur",
        ["DeletedAt"] = "Silinme tarihi (yumuşak silme)",
        ["DeletedBy"] = "Kaydı silen kullanıcı kimliği",
        ["Title"] = "Kayıt başlığı",
        ["Name"] = "Ad",
        ["Email"] = "E-posta adresi",
        ["Content"] = "İçerik metni",
        ["Description"] = "Açıklama metni",
        ["IpAddress"] = "İsteğin geldiği IP adresi",
        ["UserAgent"] = "İstemci tarayıcı bilgisi",
        ["UserId"] = "İlişkili kullanıcı kimliği",
        ["Status"] = "Durum kodu"
    };

    private static readonly Dictionary<string, string> Ozel = new(StringComparer.Ordinal)
    {
        ["Blog.Title"] = "Blog yazısının başlığı",
        ["Blog.Content"] = "Blog yazısının tam gövdesi (Markdown)",
        ["Log.Project"] = "Kaydı yazan bileşen — serbest metin, sözlüğe bağlı değil",
        ["Log.Level"] = "Kayıt seviyesi (Error / Warning / Information)",
        ["Log.Detail"] = "İstisna yığını ve ek teşhis",
        ["Log.Path"] = "İsteğin yolu",
        ["Report.TargetType"] = "Şikayetin hangi tabloya baktığı — foreign key yoktur",
        ["Report.TargetId"] = "Hedef kaydın kimliği; varlığı denetlenmez",
        ["Report.Status"] = "Pending / Reviewed / Dismissed / ActionTaken",
        ["Report.AdminNote"] = "Yöneticinin inceleme notu",
        ["Contact.IsRead"] = "Mesajın okunma durumu",
        ["User.Username"] = "Giriş adı — benzersizdir",
        ["User.PasswordHash"] = "Parolanın karması; düz metin hiçbir yerde tutulmaz",
        ["Subscriber.Email"] = "Bülten abonesinin e-postası — benzersizdir",
        ["MailTemplate.Subject"] = "E-posta konu satırı",
        ["MailTemplate.Body"] = "E-posta gövdesi (HTML)"
    };

    public static string For(string entity, string column)
    {
        if (Ozel.TryGetValue($"{entity}.{column}", out var ozel))
            return ozel;

        return Ortak.TryGetValue(column, out var ortak) ? ortak : "—";
    }
}
