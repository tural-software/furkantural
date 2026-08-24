using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

/// <summary>Şablondan posta gönderen kapı. Çağıran yalnızca tür kodunu, alıcıyı ve o türün gövde DTO'sunu verir; şablonun bulunması, konu ile gövdenin doldurulması ve SMTP'ye teslim buranın işidir.<para><see cref="IEmailService"/> ile karıştırılmamalı: o, verilen konuyu ve HTML'i olduğu gibi yollayan alt seviyedir. Buradaki gönderim metni veri tabanındaki şablondan üretir, dolayısıyla metin değişikliği kod değişikliği gerektirmez.</para><para>Sonuç bilerek <see cref="Result"/>'tır, istisna değil: şablonun eksikliği ya da SMTP arızası çağıranın kararına bırakılır. İletişim formu postayı yutup akışı sürdürür, hesap aktivasyonu ise sürdüremez — gönderilemeyen bir bağlantı kullanıcıyı hiç ulaşmayacak bir postayı beklemeye bırakırdı.</para></summary>
public interface IMailSender
{
    Task<Result> SendAsync(string typeCode, string? toEmail, object payload, CancellationToken cancellationToken = default);
}

/// <summary>Şablon metnindeki <c>{{Ad}}</c> ifadelerini gövde DTO'sunun aynı adlı özellikleriyle değiştirir; eşleşme büyük/küçük harfe duyarlıdır.<para>Karşılığı olmayan yer tutucu boşa indirilir ve uyarı olarak kaydedilir. Olduğu gibi bırakmak <c>{{Foo}}</c> ifadesinin müşteriye giden postada görünmesi demek olurdu; sessizce silmek ise yazım hatasını gizlerdi.</para></summary>
public interface IMailRenderer
{
    string Render(string? template, object payload);
}
