using FurkanTural_Domain.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FurkanTural_Business.Helpers;

/// <summary>Kayıt kaynağını isteğin kendi rotasından türetir: bileşen controller adı, işlem action adı ve HTTP fiilidir. Böylece <see cref="LogSources"/> biçimi doksan yedi çağrı noktasında elle yazılmaz ve yeni bir uç açıldığında kendiliğinden doğru adlandırılır.<para>label verilirse rota yok sayılır; iş anlamı rotadan farklı olan çağrılar (tek uçtan birden çok işi yürüten toplu uçlar gibi) kendi etiketini verebilsin diye.</para><para>İstek bağlamı yoksa — arka plan çağrısı, birim testi — yalnızca uygulama adı döner. Bu "bileşen bilgisi yoktu" demektir; uydurma bir bileşen adı yazmaktan iyidir.</para></summary>
public static class LogSourceBuilder
{
    public static string FromContext(HttpContext? context, string app, string? label = null)
    {
        if (!string.IsNullOrWhiteSpace(label))
            return LogSources.Compose(app, label);

        var route = context?.GetRouteData().Values;
        return LogSources.Compose(
            app,
            route?["controller"] as string,
            route?["action"] as string,
            Verb(context?.Request.Method));
    }

    private static string? Verb(string? method)
        => string.IsNullOrWhiteSpace(method)
            ? null
            : char.ToUpperInvariant(method[0]) + method[1..].ToLowerInvariant();
}
