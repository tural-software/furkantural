using FurkanTural_Blog.Models;
using FurkanTural_Blog.Services;
using Moq;

namespace FurkanTural_Blog.Tests;

/// <summary>Yazı sayfasının çizilmesi için gerekli ama sınanan davranışla ilgisi olmayan bağımlılıklar. Yorum istemcisi boş bir bölüm döndürür: <c>Mock.Of</c>'un varsayılanı <c>null</c> taşıyan bir görev olurdu ve o, yönlendirme sınayan testleri ilgisiz bir başvuru hatasıyla kırardı.</summary>
internal static class BlogStubs
{
    public static ICommentClient EmptyComments() => Mock.Of<ICommentClient>(c =>
        c.GetThreadAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()) == Task.FromResult(new CommentThreadViewModel()));
}
