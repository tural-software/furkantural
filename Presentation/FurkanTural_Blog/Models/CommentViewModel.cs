using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace FurkanTural_Blog.Models;

/// <summary>Sayfada çizilen tek bir yorum. API'den gelen biçimin lokal kopyasıdır ve <b>adres alanı yoktur</b>: uç onu hiç döndürmez, dolayısıyla burada tutulacak bir yer de olmamalıdır.</summary>
public sealed class CommentViewModel
{
    private static readonly CultureInfo Tr = new("tr-TR");

    public int Id { get; set; }
    public int BlogId { get; set; }
    public int? ParentId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    /// <summary>Yazının sahibinden gelen yanıt mı. Okur için önemli bir ayrım: aynı görünen iki satırdan hangisinin cevap sayılacağını söyler.</summary>
    public bool IsAuthor { get; set; }

    public List<CommentViewModel> Replies { get; set; } = [];

    public int Depth { get; set; }

    public string? ParentAuthorName { get; set; }

    public string PublishedDisplay =>
        CreatedAt == default ? string.Empty : CreatedAt.ToString("d MMMM yyyy HH:mm", Tr);

    public string PublishedIso =>
        CreatedAt == default ? string.Empty : CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ");

    /// <summary>Sayfada kullanılan çapa. Bildirim postasındaki bağlantı de bu biçimi kullanır; ikisi birlikte değişmelidir.</summary>
    public string Anchor => $"yorum-{Id}";
}

/// <summary>Bir yazının yorum bölümü. Sayfada tek parça çizilir; sayfalama kök yorumlara uygulanır ve bir kökün yanıt zinciri, kaç seviye sürerse sürsün, daima onunla birlikte gelir.</summary>
public sealed class CommentThreadViewModel
{
    public int BlogId { get; set; }
    public int TotalCount { get; set; }
    public int RootCount { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public List<CommentViewModel> Items { get; set; } = [];

    /// <summary>Sayfaya sığmayan kök yorum var mı. Varsa okura bir uyarı çizilir; sessizce kesmek, konuşmanın orada bittiği izlenimini verirdi.</summary>
    public bool HasMore => TotalPages > 1;

    public void StampTree()
    {
        Walk(Items, 0, null);

        static void Walk(List<CommentViewModel> nodes, int depth, string? parentName)
        {
            foreach (var node in nodes)
            {
                node.Depth = depth;
                node.ParentAuthorName = parentName;
                Walk(node.Replies, depth + 1, node.AuthorName);
            }
        }
    }
}

/// <summary>Yorum formunun kendisi. Gönderim sayfayı yeniden çizer, dolayısıyla model hem girdiyi hem sonucu taşır.</summary>
public sealed class CommentFormModel
{
    /// <summary>Yorumun bırakıldığı yazı. Gizli alandan gelir; sayfanın kendisi zaten yazının sayfasıdır.</summary>
    public int BlogId { get; set; }

    [Required(ErrorMessage = "Adınızı girin.")]
    [StringLength(80, ErrorMessage = "Ad en fazla 80 karakter olabilir.")]
    public string? AuthorName { get; set; }

    [Required(ErrorMessage = "E-posta adresi gerekli.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
    public string? AuthorEmail { get; set; }

    [Required(ErrorMessage = "Yorumunuzu yazın.")]
    [StringLength(4000, MinimumLength = 2, ErrorMessage = "Yorum en az 2, en fazla 4000 karakter olmalı.")]
    public string? Body { get; set; }

    /// <summary>Yanıt verilen yorum. Boşsa yorum yazıya doğrudan bırakılır.</summary>
    public int? ParentId { get; set; }

    /// <summary>Yanıt geldiğinde posta istenip istenmediği. Varsayılan kapalıdır ve formda açıkça işaretlenir.</summary>
    public bool NotifyOnReply { get; set; }

    /// <summary>Tuzak alan; gerekçesi <see cref="NewsletterViewModel.Website"/> ile birebir aynıdır.</summary>
    public string? Website { get; set; }

    public string? TurnstileToken { get; set; }

    public bool Submitted { get; set; }
    public bool Succeeded { get; set; }
    public string? ResultMessage { get; set; }
}

/// <summary>Bildirim kapatma bağlantısının indiği sayfa. Jeton adres satırından gelir; sayfa yalnızca sonucu gösterir.</summary>
public sealed class CommentNotificationViewModel
{
    public bool Succeeded { get; set; }
    public string? Message { get; set; }
    public bool TokenMissing { get; set; }
}
