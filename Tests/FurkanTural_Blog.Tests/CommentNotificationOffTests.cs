using FluentAssertions;
using FurkanTural_Blog.Controllers;
using FurkanTural_Blog.Models;
using FurkanTural_Blog.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FurkanTural_Blog.Tests;

/// <summary>Yorum bildirimini kapatma bağlantısı eskiden açıldığı anda işlem yapıyordu. Posta güvenlik tarayıcıları bağlantıları kendiliğinden açtığı için bildirimler okur tıklamadan kapanabiliyordu.</summary>
public class CommentNotificationOffTests
{
    [Fact]
    public void Baglantiyi_acmak_bildirimleri_kapatmaz()
    {
        var comments = new Mock<ICommentClient>(MockBehavior.Strict);

        var view = (ViewResult)new CommentController(comments.Object).NotificationOff(" jeton ");

        var model = view.Model.Should().BeOfType<CommentNotificationViewModel>().Subject;
        model.Pending.Should().BeTrue();
        model.Token.Should().Be("jeton");
    }

    [Fact]
    public async Task Dugme_bildirimleri_kapatir()
    {
        var comments = new Mock<ICommentClient>();
        comments.Setup(c => c.DisableNotificationsAsync("jeton", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommentOutcome(true, "Kapatıldı."));

        var view = (ViewResult)await new CommentController(comments.Object).NotificationOffPost("jeton", default);

        view.Model.Should().BeOfType<CommentNotificationViewModel>().Which.Succeeded.Should().BeTrue();
        comments.Verify(c => c.DisableNotificationsAsync("jeton", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Kapatan_uc_yalnizca_sahtecilik_jetonlu_POST_kabul_eder()
    {
        var method = typeof(CommentController).GetMethod(nameof(CommentController.NotificationOffPost))!;

        method.GetCustomAttributes(typeof(HttpPostAttribute), false).Should().NotBeEmpty();
        method.GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), false).Should().NotBeEmpty();
    }
}
