using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Domain.Entities;

/// <summary>Uygulama bülteni abonesi.<para>Listeye girmek iki adımlıdır: kayıt açılır ama <see cref="ConfirmedAt"/> boş kaldığı sürece adres <b>doğrulanmamış</b>tır ve bülten gönderimine dâhil edilmez. Doğrulama <see cref="SubscriberVerification"/> jetonuyla yapılır; böylece kimse başkasının adresini listeye yazdıramaz.</para></summary>
public class Subscriber : BaseEntity
{
    public string? Email { get; set; }

    /// <summary>Adresin sahibinin doğrulama bağlantısına tıkladığı an. Boşsa kayıt vardır ama abonelik yoktur — gönderim yalnızca dolu olanlara yapılır.</summary>
    public DateTime? ConfirmedAt { get; set; }
}
