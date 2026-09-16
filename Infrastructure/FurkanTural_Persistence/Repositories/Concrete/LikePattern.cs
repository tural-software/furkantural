using System.Text;

namespace FurkanTural_Persistence.Repositories.Concrete;

/// <summary>Ham SQL'de <c>LIKE</c> ile "içerir" araması kurar. Aranan metin parametreyle gittiği için enjeksiyon zaten yoktur; sorun joker karakterlerdir: <c>%</c>, <c>_</c> ve <c>[</c> kaçışlanmazsa kullanıcının yazdığı metin desenin kendisi olur. <c>%_%_%_%</c> gibi bir arama bütün tabloyu sıralı taramaya zorlar ve eşleşmesi gerekmeyen satırları da getirir.<para>Kaçış karakteri ters eğik çizgidir ve SQL tarafında <see cref="EscapeClause"/> ile bildirilmelidir; bildirilmezse SQL Server ters eğik çizgiyi sıradan karakter sayar ve kaçışlanan her joker aramayı bozar.</para></summary>
public static class LikePattern
{
    public const string EscapeClause = " ESCAPE '\\'";

    public static string Contains(string value)
    {
        var builder = new StringBuilder(value.Length + 2).Append('%');

        foreach (var character in value)
        {
            if (character is '\\' or '%' or '_' or '[')
                builder.Append('\\');

            builder.Append(character);
        }

        return builder.Append('%').ToString();
    }
}
