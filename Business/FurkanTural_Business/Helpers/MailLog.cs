using FurkanTural_Application.Wrappers;

namespace FurkanTural_Business.Helpers;

public static class MailLog
{
    public static string Sent(string mail) => $"{mail} gönderildi.";

    public static string Failed(string mail, string reason) => $"{mail} gönderilemedi: {reason.TrimEnd('.', ' ')}.";

    public static string Skipped(string mail, string reason) => $"{mail} gönderilmedi: {reason.TrimEnd('.', ' ')}.";

    public static string Describe(string mail, Result sent)
        => sent.Success ? Sent(mail) : Failed(mail, Reason(sent));

    /// <summary>Başarısızlığın kayda geçecek metni. Zarf, iç mesajı boş bir dize olarak taşıyabilir ve o hâlde hata sütununa boşluk yazılırdı; sütun bir bildirimin niçin gitmediğini söyleyen tek kayıt olduğu için sırayla en açıklayıcı olan seçilir.</summary>
    public static string Reason(Result sent)
        => !string.IsNullOrWhiteSpace(sent.InternalMessage) ? sent.InternalMessage
         : sent.Errors.FirstOrDefault(e => !string.IsNullOrWhiteSpace(e))
           ?? (string.IsNullOrWhiteSpace(sent.Message) ? "Posta gönderilemedi." : sent.Message);
}
