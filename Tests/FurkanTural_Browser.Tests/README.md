# Tarayıcı Tarama Katmanı

Bu proje, dört sunum uygulamasının sayfalarını **gerçek bir tarayıcıda** açıp ölçer.
Kaynak metnini okuyan testlerin göremeyeceği kusurlar için var: yerleşim taşması,
kontrast, konsol hatası, dokunma hedefi boyutu, başlık yapısı.

Kaynak düzeyindeki denetimler `FurkanTural_DesignSystem.Tests` içinde kalır; burası
yalnızca çalışan siteyi ölçer.

## Çalıştırma

Beş servis ayakta olmalı (API 7000, Admin 7001, Portfolio 7002, Blog 7003, Chat 7004):

```powershell
dotnet test Tests\FurkanTural_Browser.Tests
```

Tarayıcı olarak kurulu **Edge** kullanılır, bulunamazsa Chrome denenir; ayrıca tarayıcı
indirmek gerekmez. Ayakta olmayan uygulamanın sayfaları **atlanır**, başarısız olmaz —
ortam eksikliği ile kusur birbirine karışmasın diye.

## Ortam değişkenleri

| Değişken | Ne işe yarar | Yoksa |
|---|---|---|
| `SWEEP_CHAT_USER` / `SWEEP_CHAT_PASS` | Chat oturumlu sayfaları | O sayfalar atlanır |
| `SWEEP_ADMIN_USER` / `SWEEP_ADMIN_PASS` | Admin bölümleri | O sayfalar atlanır |
| `SWEEP_URL_CHAT` vb. | Adresi değiştirir | `localhost:700x` |
| `SWEEP_BROWSER_CHANNEL` | Tarayıcı kanalını sabitler | `msedge`, sonra `chrome` |
| `SWEEP_HEADED` | `1` ise tarayıcı görünür açılır | Başsız |

Kimlik bilgileri koda ve depoya yazılmaz; yalnızca ortamdan okunur.

## Ölçülenler

| Sınıf | İddia |
|---|---|
| `LayoutSweepTests` | Yatay kaydırma yok; hiçbir kapsayıcı yanlamasına taşmaz; dokunma hedefleri ≥ 24×24 (WCAG 2.2 - 2.5.8, aralık istisnası uygulanır); altbilgi ana içeriğin üstüne binmez |
| `StructureSweepTests` | Tam bir `h1`; başlık seviyesi atlanmaz; `lang` ve `title` dolu; yinelenen `id` yok |
| `AccessibilitySweepTests` | Görsellerin `alt`'ı; form denetimlerinin ve bağlantıların erişilebilir adı var |
| `ContrastSweepTests` | Metin kontrastı WCAG AA eşiğini geçer (iki temada da) |
| `ConsoleSweepTests` | Konsola hata yazılmaz; hiçbir kaynak başarısız olmaz |
| `NavigationSweepTests` | Yanıt < 400; oturumlu sayfa giriş ekranına düşmez |
| `KeyboardSweepTests` | Tab ile gezilen her durakta odak görünür değişir; odaklanan öge ekranda kalır; klavye tuzağı yok; pozitif `tabindex` yok; tekrar eden gezinme atlanabilir; sayfanın tek bir `main` bölgesi var |
| `ConsentGateTests` | Çerez onayı ilk ziyarette çıkar, bir kez kabul edilince bir daha sorulmaz; onay çerezi yazılır ve katman sunucudan hiç gelmez; katman hiçbir betik çalışmadan ekrandadır; önceden onay vermiş ziyaretçide hiç görünmez |
| `RealtimeTests` | Sohbet ekranı BFF üzerinden gerçek bir WebSocket açar |
| `FlowTests` | Blog araması sonuca götürür; proje kartı detaya götürür; yönetim modalı odağı içeride tutar ve Escape ile kapanır; tema seçimi sayfa değişince korunur; uygulama içi yasal belge sayfaya ait düğmeleri taşımaz; onay penceresi uygulama içinde açılır ve Escape ile vazgeçilir |
| `LayoutBaselineTests` | İzlenen sayfaların yerleşimi onaylı temelden sapmaz |

Her sayfa (genişlik, tema) başına **bir kez** açılır; ölçüm önbelleğe alınır ve bütün
sınıflar aynı ölçümü okur.

## Genişletme

Yeni sayfa eklemek için `Infrastructure/SiteMap.cs` içindeki listeye bir satır yazmak
yeterlidir; bütün denetimler o sayfayı kendiliğinden kapsar. Kimliği ortamdan ortama
değişen detay sayfaları `Discover` seçicisiyle çalışma anında bulunur.

Ölçümün kendisi `Infrastructure/probe.js` içindedir ve sayfa bağlamında çalışır.

### Bilerek yanlamasına kayan kutular

`probe.js` içindeki `ALLOWED_SCROLLERS` listesi, yatay kaymanın kasıtlı olduğu kutuları
tutar (kod blokları, geniş tablolar). Sayfa düzeyinde (`html`/`body`) taşma her hâlükârda
kusurdur; iç kutularda yalnızca kullanıcıyı yatay kaydırmaya zorlayan `overflow-x: auto`
veya `scroll` bildirilir. Üç nokta kırpması, ekran-okuyucu metni ve ekran dışında bekleyen
paneller (`overflow-x: hidden`) kasıtlı olduğu için bildirilmez.

### Klavye yürüyüşü

Odak halkası ölçülürken sayfaya `transition/animation/scroll-behavior: none` enjekte edilir;
aksi hâlde geçiş henüz başlamadan ölçüm yapılır ve var olan halka yok görünür. Her durağın
odaklıyken ve odaksızken hesaplanan stili karşılaştırılır, yani halkanın kendisi değil
**değişim** aranır. Cloudflare Turnstile'ın kendi ögeleri kapsam dışıdır.

Bir sayfa bir alana `autofocus` veriyorsa "ilk Tab durağı atlama bağlantısıdır" iddiası
atlanır: odak zaten formun içinde başlar.

### Yerleşim temeli

`Baselines/` altında her izlenen sayfa ve genişlik için bir metin dosyası durur: görünür
ögelerin yolu ve kutusu (`x,y,g,y`). Ekran görüntüsü yerine metin, çünkü fark git'te
okunabilir, yazı tipi çizim farklarından etkilenmez ve ek bağımlılık gerektirmez. Renk
değişimini yakalamaz; onu `ContrastSweepTests` ve token testleri kovalar.

Temel yoksa yazılır ve test atlanır. Değişiklik kasıtlıysa:

```powershell
$env:SWEEP_UPDATE_BASELINES = '1'; dotnet test Tests/FurkanTural_Browser.Tests
```

İzlenen sayfalar içeriği veriye bağlı olmayanlarla sınırlıdır; liste sayfaları kayıt
sayısıyla birlikte oynadığı için temele alınmaz. Giriş ve kayıt sayfaları da dışarıdadır:
robot doğrulaması yüklenemediğinde sayfa iki satırlık bir uyarı gösterir ve kart kayar, yani
yerleşimleri Cloudflare'a erişilip erişilemediğine bağlıdır. Bu sayfalar diğer bütün
denetimlerde yine taranır. Turnstile widget'ının kendisi ölçüm dışıdır; yüksekliği CSS'te
ayrıldığı için doğrulama yüklendiğinde form sıçramaz.

### Çerez onayının zamanı

Katmanı **sunucu** açık basar. Görünürlüğü betiğe bırakmak, pencerenin sayfa boyandıktan
sonra üstüne düşmesi demekti: kullanıcı önce siteyi görüyor, sonra kutu patlıyordu. Onay
çerezde durduğu için sunucu kararı isteği alırken verebiliyor; kabul edilmişse katman
HTML'e hiç girmiyor. `Katman_hicbir_betik_calismadan_ekranda` bunu JavaScript kapalı bir
ziyaretle ölçer — betik çalışmadan görünüyorsa gecikme de yok demektir.

Onayı yalnızca localStorage'da olan eski ziyaretçi için `<head>`'de tek bir betik var:
çerezi yazar ve `data-consent` niteliğini koyar, CSS de katmanı ilk boyamadan önce gizler.
Bu yol `Onceden_onay_vermis_ziyaretcide_katman_hic_gorunmez` ile ayrıca ölçülür.

### Üst üste binen bölgeler

Taşma denetimi yalnızca **yanlamasına** kaçan içeriği görür. Bir kutu kendi yüksekliğine
sığmayıp altındaki bölgenin arkasına akarsa sayfa yatay kaydırma üretmez, kapsayıcı da
yanlamasına taşmaz — ama metin okunmaz olur. `LayoutSweepTests` bu yüzden ana içerik ile
altbilginin kutularını ayrıca karşılaştırır ve örtüşmenin kaç piksel olduğunu, hangi
kapsayıcının taştığını söyler. Bildirimde en dıştaki suçlu verilir; ebeveyni de örtüşen
ögeler tekrar sayılmaz. Kendi katmanında duran (`fixed`, `sticky`, `absolute`) ögeler
kapsam dışıdır: onların üstte durması tasarımın kendisidir.

### Etkileşim akışları

`FlowTests` yalnızca **okuma** yapan yolculukları sürer: arama, gezinme, modal açma,
tema değiştirme. Kayıt açan/silen akış yoktur; test verisi birikmesin diye.

## Bilinen sınırlar

- Chat girişi Cloudflare Turnstile'a bağlıdır; Development ortamı her zaman geçen test
  anahtarlarını kullanır. Cloudflare'a erişilemezse Chat oturumlu sayfaları atlanır.
- Kontrast ölçümü, arkasında `background-image` veya `opacity < 1` bulunan metinleri
  "ölçülemez" sayar ve atlar; bunlar `Unmeasurable` alanında toplanır.
- Tarama, çerez onayı verilmiş bir ziyaretçiyi taklit eder. Onay katmanının kendisi
  `ConsentGateTests` ile ayrıca denetlenir.
- Oturum kurulduğu **adresle değil DOM ile** doğrulanır: Admin'in giriş ekranı kök adreste
  durduğu için adreste "login" geçmez ve adrese bakan bir denetim, giriş sayfasını o sayfa
  sanarak sahte kapsama üretir.
- Ağ kaynaklı gezinme hataları (`ERR_NETWORK_IO_SUSPENDED`, gezinme zaman aşımı) iki kez
  yeniden denenir; kalıcı bir kusur yine kırmızıya döner.
