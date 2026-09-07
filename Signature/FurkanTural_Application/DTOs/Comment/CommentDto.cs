namespace FurkanTural_Application.DTOs.Comment;

/// <summary>Okura çizilen yorum. <b>Adres taşımaz</b> ve bu bir eksiklik değil sözleşmenin kendisidir: yorum listesi herkese açık bir uçtan gelir, adresi de taşısaydı sayfayı çeken biri yorumcuların adreslerini toplayabilirdi.<para>Yanıtlar iç içe değil tek düzey gelir. <see cref="Replies"/> yalnızca kök yorumlarda doludur; bir yanıtın kendi yanıtı olamayacağı için ağaç ikinci seviyede biter ve istemcinin ağaç kurması gerekmez.</para></summary>
public class CommentDto
{
    public int Id { get; set; }
    public int BlogId { get; set; }
    public int? ParentId { get; set; }
    public string? AuthorName { get; set; }
    public string? Body { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>Yazının sahibinden gelen yanıt mı. Okur için önemli bir ayrım: aynı görünen iki satırdan hangisinin cevap sayılacağını söyler.</summary>
    public bool IsAuthor { get; set; }

    public List<CommentDto> Replies { get; set; } = [];
}

/// <summary>Bir yazının yorum bölümünün bir sayfası. Sayfalama <b>kök yorumlara</b> uygulanır ve bir kökün yanıtları daima onunla birlikte gelir; yanıtları da sayfalamak, konuşmayı sayfa sınırından ikiye bölerdi.<para>İki sayaç birden taşınır çünkü ikisi ayrı soruların cevabıdır: <see cref="TotalCount"/> başlıkta yazan "kaç yorum var", <see cref="RootCount"/> ise sayfa sayısını belirleyen değerdir. Yanıtlar köklerin içinde gömülü olduğu için biri diğerinden çıkarılamaz.</para></summary>
public class CommentThreadDto
{
    public int BlogId { get; set; }

    /// <summary>Yazının yayındaki tüm yorumları; yanıtlar dâhil.</summary>
    public int TotalCount { get; set; }

    /// <summary>Yayındaki kök yorum sayısı. Sayfalama buna göre yapılır.</summary>
    public int RootCount { get; set; }

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)RootCount / PageSize) : 0;

    public List<CommentDto> Items { get; set; } = [];
}
