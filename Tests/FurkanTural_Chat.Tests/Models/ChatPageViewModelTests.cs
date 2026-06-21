using FluentAssertions;
using FurkanTural_Chat.Models.Chat;

namespace FurkanTural_Chat.Tests.Models;

/// <summary>
/// ChatPageViewModel davranis testleri.
/// ViewModel pure data holder; hesaplamasi olmayan ama varsayilan degerleri kontrol ediyoruz.
/// </summary>
public class ChatPageViewModelTests
{
    [Fact]
    public void ChatPageViewModel_DefaultCtor_HasSaneDefaults()
    {
        // Act
        var vm = new ChatPageViewModel();

        // Assert
        vm.ApiBaseUrl.Should().BeEmpty();
        vm.UserId.Should().Be(0);
        vm.Username.Should().BeEmpty();
        vm.DisplayName.Should().BeEmpty();
        vm.AvatarUrl.Should().BeEmpty();
        vm.AgreementAccepted.Should().BeFalse();
    }

    [Fact]
    public void ChatPageViewModel_WithAllProperties_HoldsValues()
    {
        // Arrange & Act
        var vm = new ChatPageViewModel
        {
            ApiBaseUrl = "https://api.test.local",
            UserId = 7,
            Username = "furkan",
            DisplayName = "Furkan T.",
            AvatarUrl = "https://cdn.test/av.jpg",
            AgreementAccepted = true
        };

        // Assert
        vm.ApiBaseUrl.Should().Be("https://api.test.local");
        vm.UserId.Should().Be(7);
        vm.Username.Should().Be("furkan");
        vm.DisplayName.Should().Be("Furkan T.");
        vm.AvatarUrl.Should().Be("https://cdn.test/av.jpg");
        vm.AgreementAccepted.Should().BeTrue();
    }

    [Fact]
    public void ChatPageViewModel_AgreementAccepted_DefaultIsFalse()
    {
        // Yeni kullanicinin sozlesme durumu varsayilan olarak false olmali.
        var vm = new ChatPageViewModel();
        vm.AgreementAccepted.Should().BeFalse();
    }

    [Fact]
    public void ChatPageViewModel_AgreementAccepted_CanBeSetToTrue()
    {
        var vm = new ChatPageViewModel { AgreementAccepted = true };
        vm.AgreementAccepted.Should().BeTrue();
    }

    [Fact]
    public void ChatPageViewModel_UserId_Zero_WhenNotSet()
    {
        // UserId 0 => kimlik dogrulanmamis kullanici gibi davranmamali,
        // ChatController zaten token yoksa redirect yapiyor; burada model seviyesi test.
        var vm = new ChatPageViewModel();
        vm.UserId.Should().Be(0);
    }
}
