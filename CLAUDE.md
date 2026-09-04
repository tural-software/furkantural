# Proje Bağlamı

> Bu dosya `.claude/SETUP_WORKFLOW.md` keşif aşamasının çıktısıyla dolduruldu.
> Gerçek keşif bulgularını yansıtır; multi-agent `/full-audit` workflow'unun bağlam kaynağıdır.

## Solution Yapısı

> **Çözüm dosyası:** kökte `FurkanTural.slnx` (uygulama projeleri; test projeleri dahil **değil** — onlar tek tek `dotnet test` ile çalışır). `.docs/furkantural/` yalnızca **tasarım referansıdır**, audit kapsamı dışıdır.

- **API:** `Web/FurkanTural_API/` (JWT + REST + SignalR + API versioning)
- **MVC Projeler (Sunum):**
  - `Presentation/FurkanTural_Admin/` — yönetim paneli (24 controller, session-tabanlı auth, custom design-system)
  - `Presentation/FurkanTural_Chat/` — sohbet (4 controller, in-process YARP BFF `/bff/*`)
  - `Presentation/FurkanTural_Blog/` — genel blog (2 controller, app-token, kullanıcı auth yok)
  - `Presentation/FurkanTural_Portfolio/` — genel portfolyo (4 controller, app-token, kullanıcı auth yok)
- **Shared (Application):** `Signature/FurkanTural_Application/` — DTO'lar, servis/repo interface'leri, `Result<T>` wrapper
- **Domain (Core):** `Core/FurkanTural_Domain/` — entity'ler (BaseEntity + ISoftDeletable/IAuditable/IInsertable)
- **Business:** `Business/FurkanTural_Business/` — ~35 servis + mapper'lar (tüm iş mantığı)
- **Infrastructure (Persistence):** `Infrastructure/FurkanTural_Persistence/` — EF Core DbContext, configuration'lar, migration'lar
- **Tools:** `Tool/FurkanTural_Crypt`, `Tool/FurkanTural_PngToBase64`, `Tool/FurkanTural_ImageConverter` (konsol, audit dışı)
- **Tests:** kök `Tests/` altında **4 xUnit projesi** (`FurkanTural_Admin.Tests`, `_Blog.Tests`, `_Portfolio.Tests`, `_Chat.Tests`) — `/full-audit` turlarında eklendi. `.slnx`'e dahil değil; tek tek `dotnet test` ile koşar.

## Teknoloji Stack
- Framework: **ASP.NET Core / .NET 10.0** (`net10.0`), nullable + implicit usings açık
- ORM: **EF Core** (Code-First; migration'lar `Infrastructure/FurkanTural_Persistence/Migrations/`; başlangıçta otomatik `Database.Migrate()` — `Database:ApplyMigrationsOnStartup`)
- Auth:
  - **API** → JWT Bearer + policy'ler (`AdminOnly`, `UserOrAdmin`, `VisitorOrAbove`, `AppClient`/`app_source` claim)
  - **Admin** → Session tabanlı (`Session["token"]`'da JWT; ASP.NET authentication şeması **yok**, kontrol controller içinde inline)
  - **Chat** → BFF: in-process YARP reverse proxy `/bff/*`, session'daki JWT'yi Authorization header'a enjekte eder (token tarayıcıya sızmaz)
  - **Blog/Portfolio** → kullanıcı auth yok; API'ye **app-token** ile
- Frontend: **Bootstrap 5** + jQuery + jquery-validation(-unobtrusive). Admin ayrıca **custom CSS bileşen sistemi** (`wwwroot/css/components/`, `site.css`) + sayfa-bazlı JS (`wwwroot/js/pages/*`)
- Realtime: **SignalR** `ChatHub` (`/hubs/chat`), UTC 'Z' kanonik tarih serileştirme
- Test: **xUnit + Moq + FluentAssertions** (kurulu; Web SDK host bağımlılığını çekmemek için ya `<Compile Include>` ile kaynak alınır ya da ProjectReference kullanılır)

## Mimari Not (KRİTİK)
Bu **API-merkezli katmanlı mimari**: MVC sunum projeleri DB'ye/EF'e **dokunmaz**. Her MVC kendi `Services/*ApiClient` sınıflarıyla ve **kendi lokal DTO/ViewModel kopyalarıyla** (örn. `Presentation/FurkanTural_Admin/Models/**`) API'ye **HTTP** üzerinden gider. Tüm iş mantığı + EF, API'nin tükettiği **Business + Persistence** katmanlarında merkezîdir.

> Sonuç: Performans (N+1, AsNoTracking, IQueryable) ve unit-test çalışmaları esas olarak **Business + Persistence (API tarafı)** içindir, MVC içinde değil.

## Kritik Kurallar
- **Kod yorumu yazma:** Yeni kod yorumu (`//`, `/* */`, `@* *@`, `<!-- -->`, `#`) **eklenmez**. Gerekçe, tasarım kararı ve ölçüm sonucu koda değil, kullanıcıya verilen yanıta yazılır. Yorum yalnızca kullanıcı açıkça isterse eklenir. Mevcut yorumlar kendiliğinden **silinmez** de; temizlik ayrı bir iştir ve kullanıcının yönlendirmesiyle parça parça yapılır.
- **API kontratı = salt-okunur:** Mevcut endpoint imzaları (route, HTTP verb, parametreler) ve DTO property'leri **değiştirilemez**. MVC projeleri bu DTO'ların lokal kopyalarıyla deserialize eder; kontrat değişimi sessizce kırar. (Yeni opsiyonel alan eklemek uyumlu olabilir → api-guardian onayı.)
- **appsettings.json'a dokunma:** Secret'lar placeholder/şifreli (`0000:base64:0000` AES deseni startup'ta çözülür). Bulguları yalnızca **raporla**, değiştirme.
- **Domain entity değişimi = migration riski:** `Core/FurkanTural_Domain` veya `Persistence/Configurations` değişimi yeni migration gerektirir → orchestrator'a eskalat. Tasarım zamanı fabrikası **yoktur**; DbContext API'nin host'undan çözülür, dolayısıyla `--startup-project` zorunludur:

  ```powershell
  dotnet ef migrations add <Ad> --project Infrastructure\FurkanTural_Persistence --startup-project Web\FurkanTural_API
  ```

  Bağlantı dizesi `appsettings`'ten gelir ve `Program.cs`'teki AES bloğu `builder.Build()`'dan önce çözer; `Database.Migrate()` ise `Build()`'dan sonra olduğu için `dotnet ef` sırasında tetiklenmez.
- **Auth konfigürasyonu (Program.cs)** değiştirilmeden önce raporlanır.
- Prod'a **Docker/ayrı servis kurulamaz** — çözümler in-process olmalı.
- **Veri çekim kuralı:** Sayfaya yansıyan her koleksiyon çekimi **sayfalı + filtreli** gelir; tüm liste ancak belirli bir sebeple (açılır liste sözlüğü, sitemap, tek sayfalık katalog) çekilir. Yönetici aktif+pasif+silinmişi görür (`admin/paged`, `admin/counts`, `AdminOnly`); yönetici olmayan yalnızca aktif satırı görür (global sorgu süzgeci). Yeni bir liste ucu/sayfası bu kalıbı izler; bkz. `.docs/plans/veri-cekim-kurali-done.md`.

## Agent Erişim Haritası

> API-merkezli mimariye **uyarlandı** (onaylı): implementasyon yazılabilir; **kontrat** (DTO + endpoint imzaları) api-guardian korumasında salt-okunur.

| Proje | Okuma | Yazma |
|---|---|---|
| `Web/FurkanTural_API` (implementasyon) | ✅ | ✅ (security, performance, test) |
| `Web/FurkanTural_API` (endpoint imzaları/route) | ✅ | ❌ (kontrat — api-guardian onayı) |
| `Business/FurkanTural_Business` | ✅ | ✅ (security, performance, test) |
| `Infrastructure/FurkanTural_Persistence` | ✅ | ✅ (performance; migration → eskalat) |
| `Signature/FurkanTural_Application` (DTO'lar) | ✅ | ❌ (kontrat — api-guardian onayı) |
| `Signature/FurkanTural_Application` (interface'ler) | ✅ | ⚠️ (dikkatle; kontrat değilse) |
| `Core/FurkanTural_Domain` (entity) | ✅ | ❌ (migration riski → eskalat) |
| `Presentation/FurkanTural_Admin` | ✅ | ✅ (ux, security, test) |
| `Presentation/FurkanTural_Chat` | ✅ | ✅ (ux, security, test) |
| `Presentation/FurkanTural_Blog` | ✅ | ✅ (ux, security, test) |
| `Presentation/FurkanTural_Portfolio` | ✅ | ✅ (ux, security, test) |
| `Tool/*`, `.docs/*` | ✅ | ❌ (kapsam dışı) |

## Keşif Notları
- **Test yok:** Gerçek solution'da hiçbir test projesi yok; en yüksek değer Business servisleri + AuthService testleri.
- **Admin auth zayıflığı:** ASP.NET authentication şeması kayıtlı değil; yetki kontrolü 24 controller'da session bazlı inline yapılıyor → tutarlılık ve `[ValidateAntiForgeryToken]`/CSRF kapsamı incelenmeli.
- **Lokal DTO kopyaları:** Her MVC kendi `Models/**` DTO kopyalarını tutar → API kontrat değişimi en büyük breaking-change kaynağı.
- **Güvenlik başlıkları tutarsız:** Blog'da X-Content-Type-Options/Referrer-Policy/X-Frame-Options var; diğer sunum projelerinde teyit edilmeli.
- **Config şifreleme + otomatik migration:** API startup'ta AES ile şifreli config çözüyor ve bekleyen migration'ları otomatik uyguluyor (fail-fast).
