using System.Security.Cryptography;
using System.Text;
using FurkanTural_Application.Settings;

namespace FurkanTural_Business.Helpers;

/// <summary>Uygulama jetonunu onu üreten anahtara bağlayan kimlik. Jeton, hangi AppKey ile alındığını bu değerle taşır; API her istekte değeri yapılandırmadaki güncel anahtarlardan yeniden hesaplayıp karşılaştırır. Anahtar döndürüldüğünde ya da uygulama listeden çıkarıldığında eski jetonlar süresini beklemeden geçersizleşir.<para>Değer anahtarın kendisinden değil, imza sırrıyla kurulan bir HMAC'ten gelir: jetonun içi herkesçe okunabilir ve anahtarın düz bir özeti orada durursa kısa ya da tahmin edilebilir anahtarlar çevrimdışı denenebilirdi.</para></summary>
public static class AppKeyIds
{
    public static string For(string signingSecret, string appName, string appKey)
        => Convert.ToHexString(HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(signingSecret),
            Encoding.UTF8.GetBytes(appName + "\n" + appKey)))[..32];

    public static bool Matches(string signingSecret, AppTokenSettings settings, string appName, string keyId)
    {
        var presented = Encoding.ASCII.GetBytes(keyId);

        return settings.Apps.Any(app =>
            string.Equals(app.AppName, appName, StringComparison.Ordinal)
            && CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(For(signingSecret, app.AppName, app.AppKey)),
                presented));
    }
}
