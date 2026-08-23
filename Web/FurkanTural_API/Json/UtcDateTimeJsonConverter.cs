using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FurkanTural_API.Json;

/// <summary>Tel üzerindeki biçimi sabitler: her tarih <c>yyyy-MM-ddTHH:mm:ss.fffZ</c> olarak yazılır, yani çıktı daima 'Z' ile biter ve milisaniye alanı hep üç hanedir. Okurken belirsiz gelen değer UTC sayılır, yerel gelen değer çevrilir; böylece hangi istemci ne gönderirse göndersin bağlam içeriye tek bir biçimde girer.</summary>
public sealed class UtcDateTimeJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetDateTime();
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var utc = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
        writer.WriteStringValue(utc.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture));
    }
}

/// <summary>Nullable <see cref="DateTime"/> için <see cref="UtcDateTimeJsonConverter"/> sarmalayıcısı.</summary>
public sealed class NullableUtcDateTimeJsonConverter : JsonConverter<DateTime?>
{
    private static readonly UtcDateTimeJsonConverter Inner = new();

    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.TokenType == JsonTokenType.Null ? null : Inner.Read(ref reader, typeof(DateTime), options);

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value.HasValue) Inner.Write(writer, value.Value, options);
        else writer.WriteNullValue();
    }
}
