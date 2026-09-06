namespace FurkanTural_Blog.Models;

/// <summary>Arşiv sayfasının tamamı: yıla, yıl içinde aya göre gruplanmış yazı listesi. Sayfalama yoktur — arşivin işi taramaktır, gezinmek değil.<para>Kaynak <c>blog/sitemap</c> ucudur ve yazı gövdesi taşımaz; bu yüzden arşiv satırında okuma süresi gösterilemez. Süreyi göstermek gövdeyi çekmeyi ya da karakter sayısından tahmin üretmeyi gerektirirdi; ikincisi aynı yazı için yazı sayfasındakinden farklı bir sayı doğururdu.</para></summary>
public sealed class ArchiveViewModel
{
    public IReadOnlyList<ArchiveYear> Years { get; init; } = [];

    /// <summary>API'ye ulaşılamadı. Boş arşivden ayrı tutulur: biri "henüz yazı yok", diğeri "liste şu an alınamıyor" demektir ve ikisi aynı ekranı göstermemelidir.</summary>
    public bool LoadFailed { get; init; }

    public int TotalCount => Years.Sum(y => y.Count);

    /// <summary>Başlıksız ya da tarihsiz kayıt listeye alınmaz: metni olmayan bir bağlantı ekran okuyucuda hedefsiz kalır, tarihsiz kayıt da hangi gruba gireceğini bildirmez.</summary>
    public static ArchiveViewModel From(IEnumerable<BlogSitemapItem> items) => new()
    {
        Years = items
            .Where(i => i.CreatedAt != default && !string.IsNullOrWhiteSpace(i.Title))
            .GroupBy(i => i.CreatedAt.Year)
            .OrderByDescending(g => g.Key)
            .Select(yearGroup => new ArchiveYear
            {
                Year = yearGroup.Key,
                Months = yearGroup
                    .GroupBy(i => i.CreatedAt.Month)
                    .OrderByDescending(g => g.Key)
                    .Select(monthGroup => new ArchiveMonth
                    {
                        Number = monthGroup.Key,
                        Name = monthGroup.First().MonthName,
                        Items = monthGroup
                            .OrderByDescending(i => i.CreatedAt)
                            .ThenByDescending(i => i.Id)
                            .ToList()
                    })
                    .ToList()
            })
            .ToList()
    };
}

public sealed class ArchiveYear
{
    public int Year { get; init; }
    public IReadOnlyList<ArchiveMonth> Months { get; init; } = [];
    public int Count => Months.Sum(m => m.Items.Count);

    /// <summary>Yıl çipinin atladığı hedef. Sayı ile başlayan bir kimlik CSS seçicisinde kaçış ister, bu yüzden harfle başlar.</summary>
    public string Anchor => $"yil-{Year}";
}

public sealed class ArchiveMonth
{
    public int Number { get; init; }
    public string Name { get; init; } = string.Empty;
    public IReadOnlyList<BlogSitemapItem> Items { get; init; } = [];
}
