using FurkanTural_Blog.Models;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_Blog.Helpers;

/// <summary>Yazı adresini tek yerden üretir. Kanonik adres <c>/yazi/{slug}</c>'dır; eski <c>/Home/Post/{id}</c> yalnızca dışarıdan gelen bağlantılar için durur ve kalıcı olarak buraya yönlendirir.<para>Slug boşsa kimlik adresine düşülür. Bu dal bugün çalışmaz — sütun boş geçilemez — ama API alanı hiç göndermezse yazı bağlantısız kalmaz, yalnızca eski adresine bağlanır.</para></summary>
public static class PostUrl
{
    public static string For(IUrlHelper url, int id, string slug) =>
        slug.Length > 0
            ? url.RouteUrl("BlogPost", new { slug }) ?? "/"
            : url.Action("Post", "Home", new { id }) ?? "/";

    public static string For(IUrlHelper url, BlogPostViewModel post) => For(url, post.Id, post.Slug);

    public static string For(IUrlHelper url, BlogSitemapItem item) => For(url, item.Id, item.Slug);
}
