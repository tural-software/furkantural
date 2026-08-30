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
| `LayoutSweepTests` | Yatay kaydırma yok; hiçbir kapsayıcı yanlamasına taşmaz; dokunma hedefleri ≥ 24×24 (WCAG 2.2 - 2.5.8, aralık istisnası uygulanır) |
| `StructureSweepTests` | Tam bir `h1`; başlık seviyesi atlanmaz; `lang` ve `title` dolu; yinelenen `id` yok |
| `AccessibilitySweepTests` | Görsellerin `alt`'ı; form denetimlerinin ve bağlantıların erişilebilir adı var |
| `ContrastSweepTests` | Metin kontrastı WCAG AA eşiğini geçer (iki temada da) |
| `ConsoleSweepTests` | Konsola hata yazılmaz; hiçbir kaynak başarısız olmaz |
| `NavigationSweepTests` | Yanıt < 400; oturumlu sayfa giriş ekranına düşmez |
| `ConsentGateTests` | Çerez onayı ilk ziyarette çıkar, bir kez kabul edilince bir daha sorulmaz |

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

## Bilinen sınırlar

- Chat girişi Cloudflare Turnstile'a bağlıdır; Development ortamı her zaman geçen test
  anahtarlarını kullanır. Cloudflare'a erişilemezse Chat oturumlu sayfaları atlanır.
- Kontrast ölçümü, arkasında `background-image` veya `opacity < 1` bulunan metinleri
  "ölçülemez" sayar ve atlar; bunlar `Unmeasurable` alanında toplanır.
- Tarama, çerez onayı verilmiş bir ziyaretçiyi taklit eder. Onay katmanının kendisi
  `ConsentGateTests` ile ayrıca denetlenir.
