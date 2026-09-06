namespace FurkanTural_Domain.Constants;

/// <summary><see cref="Entities.Log.Source"/> değerinin tek üretim yeri. Biçim <c>Uygulama-Bileşen-İşlem</c>: ilk parça kaydı yazan uygulamadır, sonrakiler olayın nereden çıktığını daraltır — <c>FurkanTural_API-Blog-Create-Post</c>, <c>FurkanTural_Portfolio-Contact-Turnstile</c>.<para>Parça sayısı sabit değildir; bilinmeyen parça yazılmaz, uydurulmaz. Hiç parça yoksa değer yalnızca uygulama adıdır ve bu "bileşen bilgisi yoktu" demektir.</para><para>Parçalar ayraçtan bölünür, yani <c>"Blog-Insert"</c> tek argüman olarak da verilebilir. Her parçadan harf, rakam ve alt çizgi dışındaki her şey atılır: bu değer istemciden de gelebiliyor ve ayraç enjeksiyonu ile uydurma bir kaynak adı üretilebilirdi.</para><para>İlk parçadan sonra gelen hiçbir parça uygulama adına benzeyemez: <c>FurkanTural_</c> öneki kırpılır. Kimliğe bürünme zaten mümkün değildi — ilk parçayı sunucu koyar — ama kırpma olmasaydı sızan metin <c>FurkanTural_Admin</c> araması sonucuna karışır, aramayı kirletirdi.</para></summary>
public static class LogSources
{
    public const string Api = "FurkanTural_API";
    public const string Admin = AppPrefix + AppSourceDefinitions.Admin;
    public const string AppPrefix = "FurkanTural_";
    public const int MaxLength = 200;
    public const char Separator = '-';

    /// <summary><c>app_source</c> claim'indeki kısa kod (Chat, Blog…) için uygulama adını verir. Kod boşsa null döner; çağıran bunu "kaynak bilinmiyor" diye ele almalıdır, uydurma bir ada düşmemelidir.</summary>
    public static string? ForApp(string? appSourceCode)
    {
        var code = Clean(appSourceCode);
        return code.Length == 0 ? null : AppPrefix + code;
    }

    public static string Compose(string app, params string?[] segments)
    {
        var parts = new List<string>(segments.Length + 1);
        var first = true;
        foreach (var raw in new[] { app }.Concat(segments))
        {
            var isApp = first;
            first = false;
            if (string.IsNullOrWhiteSpace(raw)) continue;
            foreach (var piece in raw.Split([Separator, ' ', '\t', '/', '\\'], StringSplitOptions.RemoveEmptyEntries))
            {
                var clean = Clean(piece);
                if (clean.Length == 0) continue;
                if (!isApp && clean.StartsWith(AppPrefix, StringComparison.OrdinalIgnoreCase))
                    clean = clean[AppPrefix.Length..];
                if (clean.Length > 0) parts.Add(clean);
            }
        }

        var value = string.Join(Separator, parts);
        return value.Length <= MaxLength ? value : value[..MaxLength];
    }

    private static string Clean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        var kept = value.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray();
        return new string(kept);
    }
}
