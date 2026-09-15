namespace FurkanTural_Domain.Entities.Common;

/// <summary>Bir kez harcanabilen doğrulama kaydı. Harcanma anı satırda durur; dolu bir <see cref="ConsumedAt"/> o jetonun bir daha kullanılamayacağı anlamına gelir.<para>Sözleşmenin varlık nedeni tüketmenin tek bir yazma ile yapılabilmesidir: oku-kontrol-et-yaz üçlüsü aynı bağlantıda bile olsa atomik değildir ve aynı bağlantıyı iki kez tıklayan kullanıcı ya da eş zamanlı iki istek, iki okumanın da boş <see cref="ConsumedAt"/> görmesine yol açabilir.</para></summary>
public interface ISingleUseToken
{
    DateTime? ConsumedAt { get; set; }
}
