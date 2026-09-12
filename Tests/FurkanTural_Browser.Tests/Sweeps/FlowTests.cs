using FluentAssertions;
using FurkanTural_Browser.Tests.Infrastructure;
using Microsoft.Playwright;

namespace FurkanTural_Browser.Tests.Sweeps;

[Collection(BrowserSweep.Collection)]
public sealed class FlowTests(LiveSiteFixture site)
{
    [SkippableFact]
    public async Task Blogda_arama_sonuc_sayfasina_goturur()
    {
        var (url, heading, echoed) = await site.WithPageAsync(SweepData.Page("Blog/"), async page =>
        {
            await page.FillAsync(".blog-search__input", "test");
            await page.ClickAsync(".blog-search__btn");
            await page.WaitForLoadStateAsync(LoadState.Load);

            var value = await page.InputValueAsync(".blog-search__input");
            var h1 = await page.Locator("h1").First.TextContentAsync();
            return (page.Url, h1 ?? "", value);
        });

        url.Should().Contain("/ara", "arama artık kendi adresinde yaşıyor; liste sayfasının sorgu dizesinde değil");
        url.Should().Contain("q=test", "arama sorgusu adrese taşınmazsa sonuç paylaşılamaz ve geri tuşu bozulur");
        echoed.Should().Be("test", "arama kutusu ne aradığını göstermeye devam etmeli");
        heading.Should().Contain("test", "sonuç sayfasının başlığı neyin arandığını söylemeli");
    }

    /// <summary>Kategori artık sorgu dizesi değil kendi adresi. Ray'daki chip oraya götürmeli ve sayfa hangi kategoride olduğunu başlığında söylemeli — aksi hâlde adres paylaşılabilir olur ama sayfa neyi gösterdiğini söylemez.</summary>
    [SkippableFact]
    public async Task Blogda_kategori_kendi_adresine_goturur()
    {
        var (url, heading, active) = await site.WithPageAsync(SweepData.Page("Blog/"), async page =>
        {
            var chip = page.Locator(".blog-rail a[href*='/kategori/']").First;
            Skip.If(await chip.CountAsync() == 0, "Blog: kategori yok");

            var label = (await chip.TextContentAsync() ?? "").Trim();
            await chip.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.Load);

            var h1 = await page.Locator("h1").First.TextContentAsync();
            var isActive = await page.Locator(".rail-chip.is-active").CountAsync();
            return (page.Url, (h1 ?? "").Trim(), isActive);
        });

        url.Should().Contain("/kategori/", "kategori sayfasının kendi kanonik adresi olmalı");
        heading.Should().NotBeEmpty("kategori sayfası hangi kategoride olduğunu başlığında söylemeli");
        active.Should().Be(1, "rayda tam olarak bir kategori seçili görünmeli; okur nerede olduğunu ray'dan da görmeli");
    }

    /// <summary>Eski süzgeç adresleri kalıcı olarak yeni adrese taşınır. Geçici yönlendirme olsaydı arama motoru eski adresi tutmaya devam eder, iki adres aynı listeyi gösterirdi.</summary>
    [SkippableFact]
    public async Task Blogda_eski_suzgec_adresi_kalici_yonlendirir()
    {
        var (categoryUrl, categoryStatus, searchUrl) = await site.WithPageAsync(SweepData.Page("Blog/"), async page =>
        {
            var response = await page.GotoAsync(page.Url.TrimEnd('/') + "/?categoryId=1",
                new PageGotoOptions { WaitUntil = WaitUntilState.Load, Timeout = 30000 });
            var first = response?.Request.RedirectedFrom;
            var status = first is null ? 0 : (await first.ResponseAsync())?.Status ?? 0;
            var catUrl = page.Url;

            await page.GotoAsync(page.Url.Split("/kategori")[0] + "/?search=ef",
                new PageGotoOptions { WaitUntil = WaitUntilState.Load, Timeout = 30000 });
            return (catUrl, status, page.Url);
        });

        categoryUrl.Should().Contain("/kategori/");
        categoryStatus.Should().Be(301, "kalıcı taşıma sinyali verilmezse eski adres dizinde kalır");
        searchUrl.Should().Contain("/ara").And.Contain("q=ef");
    }

    [SkippableFact]
    public async Task Portfolyoda_proje_karti_detaya_goturur()
    {
        var (before, after, heading) = await site.WithPageAsync(SweepData.Page("Portfolio/"), async page =>
        {
            var link = page.Locator("a[href*='/Projects/Detail/']").First;
            Skip.If(await link.CountAsync() == 0, "Portfolio: listelenmiş proje yok");

            var start = page.Url;
            await link.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.Load);

            var h1 = await page.Locator("h1").First.TextContentAsync();
            return (start, page.Url, h1 ?? "");
        });

        after.Should().NotBe(before, "proje kartına tıklamak detay sayfasını açmalı");
        after.Should().Contain("/Projects/Detail/");
        heading.Should().NotBeEmpty("detay sayfası projeyi bir başlıkla tanıtmalı");
    }

    [SkippableFact]
    public async Task Yonetim_detay_modali_odagi_iceride_tutar_ve_escape_ile_kapanir()
    {
        var result = await site.WithPageAsync(SweepData.Page("Admin/User"), async page =>
        {
            var trigger = page.Locator("button.ft-view-btn").First;
            Skip.If(await page.Locator("button.ft-view-btn").CountAsync() == 0, "Admin/User: listelenmiş kayıt yok");

            await trigger.ClickAsync();
            await page.Locator(".dm-overlay:not(.dm-overlay--hidden)").WaitForAsync(
                new LocatorWaitForOptions { Timeout = 10000 });

            var focusInside = await page.EvaluateAsync<bool>(
                "() => !!document.activeElement && !!document.activeElement.closest('.dm-overlay')");

            await page.Keyboard.PressAsync("Escape");
            await page.Locator(".dm-overlay:not(.dm-overlay--hidden)").WaitForAsync(
                new LocatorWaitForOptions { State = WaitForSelectorState.Detached, Timeout = 10000 });

            var closed = await page.EvaluateAsync<bool>(
                "() => { const o = document.querySelector('.dm-overlay'); return !o || o.classList.contains('dm-overlay--hidden'); }");

            return (focusInside, closed);
        });

        result.focusInside.Should().BeTrue(
            "modal açılınca odak içine taşınmalı; taşınmazsa klavye kullanıcısı hâlâ arkadaki sayfada gezer");
        result.closed.Should().BeTrue("Escape modalı kapatmalı");
    }

    [SkippableFact]
    public async Task Tema_secimi_sayfa_degisince_korunur()
    {
        var (first, second) = await site.WithPageAsync(SweepData.Page("Chat/Home/Privacy"), async page =>
        {
            var before = await page.EvaluateAsync<string>("() => document.documentElement.dataset.theme");
            await page.ClickAsync("[data-theme-toggle]");
            var after = await page.EvaluateAsync<string>("() => document.documentElement.dataset.theme");

            await page.GotoAsync(SiteMap.Chat.BaseUrl + "/Home/Agreement",
                new PageGotoOptions { WaitUntil = WaitUntilState.Load, Timeout = 30000 });
            var carried = await page.EvaluateAsync<string>("() => document.documentElement.dataset.theme");

            after.Should().NotBe(before, "düğme temayı değiştirmeli");
            return (after, carried);
        });

        second.Should().Be(first,
            "tema seçimi sonraki sayfada da geçerli olmalı; kaybolursa kullanıcı her gezinmede " +
            "gözünü yakan bir sayfayla karşılaşır");
    }

    [SkippableFact]
    public async Task Uygulama_ici_yasal_belge_sayfaya_ait_dugmeleri_tasimaz()
    {
        var result = await site.WithPageAsync(SweepData.Page("Chat/Chat"), async page =>
        {
            await page.ClickAsync("#audioSettingsBtn");
            await page.Locator(".dev-legal[data-doc='agreement']").WaitForAsync(
                new LocatorWaitForOptions { Timeout = 10000 });
            await page.ClickAsync(".dev-legal[data-doc='agreement']");

            var body = page.Locator(".legal-modal-body");
            await body.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });
            await page.WaitForFunctionAsync(
                "() => { const b = document.querySelector('.legal-modal-body'); return b && b.querySelectorAll('h2').length > 0; }",
                null, new PageWaitForFunctionOptions { Timeout = 10000 });

            return await page.EvaluateAsync<int[]>(
                """
                () => {
                  const b = document.querySelector('.legal-modal-body');
                  return [b.querySelectorAll('h2').length,
                          b.querySelectorAll('.legal-actions, .legal-back').length];
                }
                """);
        });

        result[0].Should().BeGreaterThan(0, "belge gövdesi boş gelirse bu testin ikinci iddiası anlamsızlaşır");
        result[1].Should().Be(0,
            "sayfaya ait \"Geri dön\" / \"Kayıt sayfasına dön\" düğmeleri modalın içinde işe yaramaz: " +
            "oturum açmış kullanıcıyı ya kayıt ekranına ya da giriş öncesi karşılama sayfasına atarlar");
    }

    [SkippableFact]
    public async Task Onay_penceresi_uygulama_icinde_acilir_ve_escape_ile_vazgecilir()
    {
        var result = await site.WithPageAsync(SweepData.Page("Chat/Home/Privacy"), async page =>
        {
            var pending = page.EvaluateAsync<bool>(
                "() => askConfirm({ title: 'Sınama', text: 'Onay penceresi çalışıyor mu?' })");

            await page.Locator("#askDialog:not([hidden])").WaitForAsync(
                new LocatorWaitForOptions { Timeout = 5000 });

            var focusInside = await page.EvaluateAsync<bool>(
                "() => !!document.activeElement && !!document.activeElement.closest('#askDialog')");

            await page.Keyboard.PressAsync("Escape");
            var answer = await pending;

            return (focusInside, answer, hidden: await page.Locator("#askDialog").IsHiddenAsync());
        });

        result.focusInside.Should().BeTrue("pencere açılınca odak içine taşınmalı");
        result.answer.Should().BeFalse("Escape vazgeçmek demektir");
        result.hidden.Should().BeTrue("vazgeçilen pencere kapanmalı");
    }

    [SkippableFact]
    public async Task Yonetim_blog_listesi_icerik_tasimaz_detay_acilirken_ayrica_ceker()
    {
        var result = await site.WithPageAsync(SweepData.Page("Admin/Blog"), async page =>
        {
            Skip.If(await page.Locator("button.ft-view-btn").CountAsync() == 0, "Admin/Blog: listelenmiş kayıt yok");

            var embedded = await page.EvaluateAsync<bool>(
                "() => { const el = document.getElementById('__blog-rows-json'); const rows = el ? JSON.parse(el.textContent || '[]') : []; " +
                "return rows.some(r => typeof r.content === 'string' && r.content.length > 0); }");

            var response = await page.RunAndWaitForResponseAsync(
                () => page.Locator("button.ft-view-btn").First.ClickAsync(),
                r => r.Url.Contains("/Blog/Content/", StringComparison.OrdinalIgnoreCase),
                new PageRunAndWaitForResponseOptions { Timeout = 15000 });

            await page.Locator(".dm-overlay:not(.dm-overlay--hidden)").WaitForAsync(
                new LocatorWaitForOptions { Timeout = 10000 });

            var shown = await page.EvaluateAsync<string>(
                "() => { const fields = Array.from(document.querySelectorAll('.dm-overlay .dm-field')); " +
                "const f = fields.find(x => ((x.querySelector('.dm-field__label') || {}).textContent || '').trim() === 'İçerik'); " +
                "if (!f) return ''; const label = f.querySelector('.dm-field__label'); " +
                "return (f.textContent || '').replace(label ? label.textContent : '', '').trim(); }");

            return (embedded, status: response.Status, shown);
        });

        result.embedded.Should().BeFalse(
            "liste satırları içeriği taşımamalı; taşıyorsa sayfa yine yazıların tamamı kadar ağır iner");
        result.status.Should().Be(200, "detay açılırken içerik tek kayıt ucundan ayrıca çekilmeli");
        result.shown.Should().NotBeNullOrEmpty().And.NotBe("—",
            "içerik ayrıca çekildiğine göre modal onu göstermeli; boş kalırsa tembel yükleme kırıktır");
    }

    [SkippableTheory]
    [InlineData("Admin/Blog")]
    [InlineData("Admin/BlogImage")]
    [InlineData("Admin/Project")]
    [InlineData("Admin/User")]
    public async Task Yonetim_duzenleme_formunda_her_alanin_erisilebilir_adi_var(string pageId)
    {
        var nameless = await site.WithPageAsync(SweepData.Page(pageId), async page =>
        {
            Skip.If(await page.Locator("button.ft-edit-btn").CountAsync() == 0, $"{pageId}: düzenlenecek kayıt yok");

            await page.Locator("button.ft-edit-btn").First.ClickAsync();
            await page.Locator(".fm-overlay:not(.fm-overlay--hidden)").WaitForAsync(
                new LocatorWaitForOptions { Timeout = 15000 });

            return await page.EvaluateAsync<string[]>(
                "() => { const root = document.querySelector('.fm-overlay'); " +
                "const controls = Array.from(root.querySelectorAll('input:not([type=hidden]):not([type=checkbox]), textarea, select, [role=switch], [role=group]')); " +
                "const text = el => el ? (el.textContent || '').trim() : ''; " +
                "const named = el => { if (el.getAttribute('aria-label')) return true; " +
                "  const by = el.getAttribute('aria-labelledby'); if (by && by.split(' ').filter(Boolean).some(id => text(document.getElementById(id)))) return true; " +
                "  if (el.id && Array.from(root.querySelectorAll('label[for]')).some(l => l.htmlFor === el.id && text(l))) return true; " +
                "  const wrap = el.closest('label'); return !!(wrap && text(wrap)); }; " +
                "return controls.filter(el => !named(el)).map(el => el.tagName.toLowerCase() + (el.name ? '[name=' + el.name + ']' : '') + (el.getAttribute('role') ? '[role=' + el.getAttribute('role') + ']' : '')); }");
        });

        nameless.Should().BeEmpty(
            $"{pageId} düzenleme formunda etiketi kendisine bağlanmamış alan var; ekran okuyucu alanı adsız okur, " +
            "etikete tıklamak da alana odaklanmaz:" + Environment.NewLine + string.Join(Environment.NewLine, nameless.Select(n => "  - " + n)));
    }

    [SkippableFact]
    public async Task Panelde_dikkat_seridi_ile_kart_rozetleri_ayni_sayiyi_soyler()
    {
        var result = await site.WithPageAsync(SweepData.Page("Admin/Dashboard"), async page =>
        {
            return await page.EvaluateAsync<string[]>(
                "() => { const problems = []; " +
                "const items = Array.from(document.querySelectorAll('.dash-attention__item')); " +
                "const badges = Array.from(document.querySelectorAll('.entity-card__attention')); " +
                "const num = s => parseInt(String(s || '').replace(/[^0-9]/g, ''), 10); " +
                "if (items.length === 0 && badges.length > 0) problems.push('şerit yokken ' + badges.length + ' kartta rozet var'); " +
                "items.forEach(it => { const c = num(it.querySelector('.dash-attention__count')?.textContent); " +
                "  if (!(c > 0)) problems.push('şeritte sıfır ya da sayısız öge: ' + it.textContent.trim()); " +
                "  if (!it.getAttribute('href')) problems.push('şerit ögesi bağlantısız: ' + it.textContent.trim()); " +
                "  const twin = badges.find(b => b.getAttribute('href') === it.getAttribute('href')); " +
                "  if (!twin) problems.push('şerit ögesinin kartta rozeti yok: ' + it.textContent.trim()); " +
                "  else if (num(twin.textContent) !== c) problems.push('rozet ile şerit farklı sayı söylüyor: ' + it.textContent.trim() + ' / ' + twin.textContent.trim()); }); " +
                "badges.forEach(b => { if (!items.some(it => it.getAttribute('href') === b.getAttribute('href'))) problems.push('rozet var ama şeritte karşılığı yok: ' + b.textContent.trim()); }); " +
                "return problems; }");
        });

        result.Should().BeEmpty(
            "şerit ve kart rozeti aynı iki sayaçtan beslenir; biri diğerinden farklı bir şey söylüyorsa " +
            "yönetici hangisine inanacağını bilemez:" + Environment.NewLine + string.Join(Environment.NewLine, result.Select(p => "  - " + p)));
    }

    [SkippableFact]
    public async Task Modul_secici_kayit_arar_ve_bulunan_kaydin_listesine_goturur()
    {
        var result = await site.WithPageAsync(SweepData.Page("Admin/Dashboard"), async page =>
        {
            await page.Keyboard.PressAsync("Control+k");
            await page.Locator("#moduleLauncher:not([hidden])").WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });
            await page.Locator("#mlSearch").FillAsync("AsNoTracking");

            var hit = page.Locator("#mlRecords [data-ml-row]").First;
            await hit.WaitForAsync(new LocatorWaitForOptions { Timeout = 15000 });
            var text = (await hit.InnerTextAsync()).Trim();
            var href = await hit.GetAttributeAsync("href") ?? "";

            await page.Keyboard.PressAsync("ArrowDown");
            var activeText = await page.EvaluateAsync<string>(
                "() => { const a = document.querySelector('#moduleLauncher .ml__row--active'); return a ? a.innerText.trim() : ''; }");

            return (text, href, activeText);
        });

        result.text.Should().Contain("AsNoTracking",
            "yerel blog arşivinde bu başlıklı yazı var; kayıt araması onu bulup göstermeli");
        result.href.Should().Contain("/Blog").And.Contain("blogId=",
            "sözlük ucundan gelen kayıt kimliğiyle süzülmüş listeye götürmeli");
        result.activeText.Should().NotBeEmpty("ok tuşları kayıt satırlarında da gezmeli; gezmezse klavye kullanıcısı sonuca ulaşamaz");
    }

    [SkippableFact]
    public async Task Panel_dort_gosterge_tasir_ve_her_birinin_degeri_var()
    {
        var kpis = await site.WithPageAsync(SweepData.Page("Admin/Dashboard"), async page =>
            await page.EvaluateAsync<string[]>(
                "() => Array.from(document.querySelectorAll('.dash-kpis .kpi')).map(k => (k.dataset.kpi || '?') + '=' + ((k.querySelector('.kpi__value') || {}).textContent || '').trim())"));

        kpis.Should().HaveCount(4, "panel başlığında dört gösterge var: toplam kayıt, bu hafta yeni, aktif kullanıcı, bekleyen iş");
        kpis.Should().OnlyContain(k => !k.EndsWith("="), "her göstergenin bir değeri ya da tiresi olmalı, boş kutu olmamalı:" + string.Join(", ", kpis));
    }

    [SkippableTheory]
    [InlineData("Admin/Blog")]
    [InlineData("Admin/Skill")]
    [InlineData("Admin/User")]
    [InlineData("Admin/Report")]
    public async Task Listede_secim_cubugu_sayar_onay_ister_ve_vazgecince_temizlenir(string pageId)
    {
        var result = await site.WithPageAsync(SweepData.Page(pageId), async page =>
        {
            var boxes = page.Locator(".data-table .row-select");
            Skip.If(await boxes.CountAsync() < 2, pageId + ": secmek icin en az iki satir gerekir");

            var barBefore = await page.Locator(".bulk-bar").IsVisibleAsync();
            await boxes.Nth(0).CheckAsync();
            await boxes.Nth(1).CheckAsync();
            await page.Locator(".bulk-bar:not([hidden])").WaitForAsync(new LocatorWaitForOptions { Timeout = 5000 });
            var count = (await page.Locator("[data-bulk-count]").InnerTextAsync()).Trim();
            var selectedRows = await page.Locator(".data-table tr.is-selected").CountAsync();

            await page.Locator("[data-bulk-action='deactivate']").ClickAsync();
            await page.Locator(".cm-overlay:not(.cm-overlay--hidden)").WaitForAsync(new LocatorWaitForOptions { Timeout = 5000 });
            var modalText = await page.Locator(".cm-overlay .cm-record").InnerTextAsync();
            await page.Locator("#cm-cancel").ClickAsync();
            await page.Locator(".cm-overlay:not(.cm-overlay--hidden)").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached, Timeout = 5000 });
            var stillSelected = await page.Locator(".data-table .row-select:checked").CountAsync();

            await page.Locator("[data-bulk-clear]").ClickAsync();
            var barAfter = await page.Locator(".bulk-bar").IsVisibleAsync();
            var checkedAfter = await page.Locator(".data-table .row-select:checked").CountAsync();

            return (barBefore, count, selectedRows, modalText, stillSelected, barAfter, checkedAfter);
        });

        result.barBefore.Should().BeFalse("hiçbir şey seçili değilken çubuk yer kaplamamalı");
        result.count.Should().Be("2 seçili");
        result.selectedRows.Should().Be(2, "seçili satır görsel olarak da işaretlenmeli");
        result.modalText.Should().Contain("2 kayıt", "onay modalı tek kayıt değil seçim sayısını söylemeli");
        result.stillSelected.Should().Be(2, "Hayır demek seçimi bozmamalı");
        result.barAfter.Should().BeFalse("Vazgeç seçimi temizler ve çubuğu kaldırır");
        result.checkedAfter.Should().Be(0);
    }

    /// <summary>Arşiv yıl çipi kendi bölümüne atlar. Çip listeyi süzmez, sayfa içinde konumlanır — arşivin tamamı tek sayfada olduğu için bağlantı hedefinin gerçekten var olması gerekir; bulunamayan bir çapa sessizce hiçbir şey yapmaz.</summary>
    [SkippableFact]
    public async Task Blogda_arsiv_yil_cipi_kendi_bolumune_atlar()
    {
        var (chipCount, anchor, sectionExists, rowCount) = await site.WithPageAsync(SweepData.Page("Blog/arsiv"), async page =>
        {
            var chip = page.Locator(".archive-year-chip").First;
            var count = await page.Locator(".archive-year-chip").CountAsync();
            Skip.If(count == 0, "Blog: arşivde yıl yok");

            var href = await chip.GetAttributeAsync("href") ?? "";
            var target = href.StartsWith('#') ? await page.Locator($"section{href}").CountAsync() : 0;
            var rows = await page.Locator(".archive-row__link").CountAsync();
            return (count, href, target, rows);
        });

        chipCount.Should().BeGreaterThan(0);
        anchor.Should().StartWith("#", "yıl çipi sayfa içi çapadır, ayrı bir adres değil");
        sectionExists.Should().Be(1, "çipin gösterdiği yıl bölümü sayfada bulunmalı");
        rowCount.Should().BeGreaterThan(0, "arşiv sayfalanmadığı için tüm satırlar tek ekranda olmalı");
    }

    /// <summary>Bülten formu JavaScript olmadan çalışmalı: sitede doğrulama kitaplığı yok ve gönderim sayfayı yeniden çizerek sonucu aynı ekranda gösterir.<para>Tuzak alanın ölçüsü ve sekme sırası ayrı ayrı sınanır. Playwright'ın görünürlük ölçütü burada işe yaramaz: kırpılmış 1×1 piksellik bir kutu da "görünür" sayılır. İnsanın yanlışlıkla dolduramaması için gereken şey alanın gerçek bir hedef sunmaması ve klavyeyle sıraya girmemesidir.</para></summary>
    [SkippableFact]
    public async Task Bulten_formu_gonderim_sonucunu_ayni_ekranda_gosterir()
    {
        var (trapArea, trapTabIndex, method, resultText) = await site.WithPageAsync(SweepData.Page("Blog/bulten"), async page =>
        {
            var form = page.Locator("form.newsletter-form");
            Skip.If(await form.CountAsync() == 0, "Blog: bülten formu yok");

            var trap = page.Locator("#bulten-website");
            var box = await trap.BoundingBoxAsync();
            var area = box is null ? 0 : box.Width * box.Height;
            var tabIndex = await trap.GetAttributeAsync("tabindex") ?? "";
            var verb = (await form.GetAttributeAsync("method") ?? "").ToLowerInvariant();

            await trap.FillAsync("bot");
            await page.Locator("input[name='Email']").FillAsync("tarama@example.invalid");
            await page.Locator("button[type='submit']").ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.Load);

            var text = await page.Locator(".newsletter-result").First.TextContentAsync();
            return (area, tabIndex, verb, (text ?? "").Trim());
        });

        trapArea.Should().BeLessThan(16, "tuzak alan tıklanabilir bir hedef sunarsa insan da doldurur ve gönderim sessizce boşa gider");
        trapTabIndex.Should().Be("-1", "sekmeyle gezinen kullanıcı tuzağa düşmemeli");
        method.Should().Be("post", "abonelik kaydı GET ile yapılamaz");
        resultText.Should().NotBeEmpty("gönderimden sonra ekranda ne olduğunu söyleyen bir metin bulunmalı");
    }

    /// <summary>Yazının kanonik adresi slug'dır. Kanonik etiket kimlik adresini gösterirse, arama motoru için aynı yazı iki adresten var olur.</summary>
    [SkippableFact]
    public async Task Blogda_yazi_adresi_sluga_dayanir()
    {
        var (url, canonical, idLinks) = await site.WithPageAsync(SweepData.Page("Blog/"), async page =>
        {
            var link = page.Locator("a[href*='/yazi/']").First;
            Skip.If(await link.CountAsync() == 0, "Blog: yazı yok");

            await link.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.Load);

            var canon = await page.Locator("link[rel='canonical']").First.GetAttributeAsync("href");
            var legacy = await page.Locator("a[href*='/Home/Post/']").CountAsync();
            return (page.Url, canon ?? "", legacy);
        });

        url.Should().Contain("/yazi/", "yazı bağlantısı kimlik adresine değil slug adresine gitmeli");
        canonical.Should().Contain("/yazi/", "kanonik etiket slug adresini göstermeli");
        idLinks.Should().Be(0, "sayfada eski kimlik adresine bağlantı kalmamalı");
    }

    /// <summary>Liste gezinmesi tabloyu yerinde tazeler. Sayfa numarasına basmak tam yenileme yaparsa okunan filtre, kaydırma konumu ve açık olan her şey sıfırlanır; bu yüzden ölçüt adresin değişmesi değil, belgenin AYNI belge kalmasıdır.<para>Nöbetçi değişken bunun için var: tam yenileme olsaydı pencere yeniden kurulur ve o değişken kaybolurdu. Geri tuşu da aynı ölçüte tabidir — pushState ile düşen geçmiş kaydı yeni bir belge yüklememelidir.</para></summary>
    [SkippableTheory]
    [InlineData("Admin/Category")]
    [InlineData("Admin/Blog")]
    [InlineData("Admin/User")]
    [InlineData("Admin/Log")]
    [InlineData("Admin/Report")]
    public async Task Admin_listesinde_sayfa_degistirmek_belgeyi_yeniden_yuklemez(string pageId)
    {
        var result = await site.WithPageAsync(SweepData.Page(pageId), async page =>
        {
            var second = page.Locator(".pagination a.pag-btn:not(.pag-btn--off)[href*='pageNumber=2']").First;
            Skip.If(await second.CountAsync() == 0, pageId + ": ikinci sayfa yok");

            await page.EvaluateAsync("window.__sweepSentinel = 'duruyor'");

            await second.ClickAsync();
            await page.WaitForFunctionAsync("() => location.search.indexOf('pageNumber=2') !== -1");
            await page.WaitForFunctionAsync("() => !document.querySelector('[data-list-controller][aria-busy]')");

            var urlAfter = page.Url;
            var sentinelAfter = await page.EvaluateAsync<string?>("() => window.__sweepSentinel || null");
            var activeAfter = (await page.Locator(".pag-btn--active").First.InnerTextAsync()).Trim();

            await page.GoBackAsync();
            await page.WaitForFunctionAsync("() => location.search.indexOf('pageNumber=2') === -1");
            await page.WaitForFunctionAsync("() => !document.querySelector('[data-list-controller][aria-busy]')");

            var sentinelBack = await page.EvaluateAsync<string?>("() => window.__sweepSentinel || null");
            var activeBack = (await page.Locator(".pag-btn--active").First.InnerTextAsync()).Trim();

            return (urlAfter, sentinelAfter, activeAfter, sentinelBack, activeBack);
        });

        result.urlAfter.Should().Contain("pageNumber=2",
            "adres hangi sayfada olunduğunu söylemeli, yoksa filtrelenmiş bir liste paylaşılamaz");
        result.sentinelAfter.Should().Be("duruyor", "sayfa değiştirmek belgeyi yeniden yüklememeli");
        result.activeAfter.Should().Be("2", "alt bant hangi sayfanın etkin olduğunu göstermeli");
        result.sentinelBack.Should().Be("duruyor", "geri tuşu da yerinde çalışmalı, yeni bir belge yüklememeli");
        result.activeBack.Should().Be("1", "geri tuşu bir önceki sayfaya dönmeli");
    }

    /// <summary>Sayaç kutuları tablonun dışında duruyor, dolayısıyla tazelenen bölgeye girmiyorlar. Tazeleme onları güncellemezse panel, silinen kaydı listeden düşürüp sayacı eski değerde bırakır ve kullanıcıya aynı anda iki farklı gerçek gösterir.</summary>
    [SkippableTheory]
    [InlineData("Admin/Category")]
    [InlineData("Admin/Blog")]
    [InlineData("Admin/User")]
    [InlineData("Admin/Log")]
    [InlineData("Admin/Report")]
    public async Task Admin_listesinde_sayaclar_tazelemeden_sonra_dolu_kalir(string pageId)
    {
        var values = await site.WithPageAsync(SweepData.Page(pageId), async page =>
        {
            Skip.If(await page.Locator("[data-stat]").CountAsync() == 0, pageId + ": sayaç kutusu yok");

            var second = page.Locator(".pagination a.pag-btn:not(.pag-btn--off)[href*='pageNumber=2']").First;
            Skip.If(await second.CountAsync() == 0, pageId + ": ikinci sayfa yok");

            await second.ClickAsync();
            await page.WaitForFunctionAsync("() => !document.querySelector('[data-list-controller][aria-busy]')");

            return await page.EvaluateAsync<string[]>(
                "() => Array.from(document.querySelectorAll('[data-stat]')).map(n => n.dataset.stat + '=' + (n.textContent || '').trim())");
        });

        values.Should().NotBeEmpty();
        values.Should().OnlyContain(v => !v.EndsWith("="),
            "her sayaç tazelemeden sonra da bir değer taşımalı: " + string.Join(", ", values));
    }

    /// <summary>Filtre çubuğu da tabloyu yerinde tazeler. Sayfalamadan ayrı bir yol izler — parametreler bağlantının adresinden değil formun alanlarından kurulur — bu yüzden ayrıca sınanır.<para>Boş alanların adrese yazılmaması kasıtlı: sunucu için eksik ile boş aynı şeydir ve paylaşılan bağlantının okunabilir kalması tercih edilir.</para></summary>
    [SkippableFact]
    public async Task Admin_listesinde_filtrelemek_belgeyi_yeniden_yuklemez()
    {
        const string pageId = "Admin/Category";
        var result = await site.WithPageAsync(SweepData.Page(pageId), async page =>
        {
            var select = page.Locator(".filter-bar select[name='deletedFilter']").First;
            Skip.If(await select.CountAsync() == 0, "Admin/Category: silinme süzgeci yok");

            await page.EvaluateAsync("window.__sweepSentinel = 'duruyor'");

            await select.SelectOptionAsync("deleted");
            await page.Locator(".filter-actions button[type='submit']").First.ClickAsync();
            await page.WaitForFunctionAsync("() => location.search.indexOf('deletedFilter=deleted') !== -1");
            await page.WaitForFunctionAsync("() => !document.querySelector('[data-list-controller][aria-busy]')");

            var sentinel = await page.EvaluateAsync<string?>("() => window.__sweepSentinel || null");
            var kept = await page.InputValueAsync(".filter-bar select[name='deletedFilter']");
            var meta = await page.EvaluateAsync<string?>("() => (window.__categoryMeta || {}).deletedFilter || null");

            return (page.Url, sentinel, kept, meta);
        });

        result.sentinel.Should().Be("duruyor", "filtrelemek belgeyi yeniden yüklememeli");
        result.Url.Should().Contain("deletedFilter=deleted", "adres hangi süzgecin etkin olduğunu söylemeli");
        result.Url.Should().NotContain("name=&", "boş süzgeç adrese yazılmamalı");
        result.kept.Should().Be("deleted", "süzgeç çubuğu seçili değeri korumalı");
        result.meta.Should().Be("deleted",
            "sayfa durumu ortak modülle aynı gerçeği taşımalı; ayrışırsa kayıt silindikten sonraki tazeleme süzgeci düşürür");
    }
}
