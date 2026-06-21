using FluentAssertions;
using FurkanTural_Admin.Controllers;
using FurkanTural_Admin.Models.Auth;
using FurkanTural_Admin.Models.Wrappers;
using FurkanTural_Admin.Services;
using FurkanTural_Admin.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Moq;

namespace FurkanTural_Admin.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthApiClient> _authApiClientMock;
    private readonly AuthController _sut;

    public AuthControllerTests()
    {
        _authApiClientMock = new Mock<IAuthApiClient>(MockBehavior.Strict);
        _sut = new AuthController(_authApiClientMock.Object);
    }

    private static ApiResult<LoginResultModel> SuccessResult(string token = "jwt-token", string role = "Admin") =>
        new()
        {
            Success = true,
            Data = new LoginResultModel
            {
                Token     = token,
                Username  = "furkan",
                RoleName  = role,
                ExpiresAt = DateTime.UtcNow.AddHours(8)
            }
        };

    private static ApiResult<LoginResultModel> FailResult(string message = "Hatalı giriş.") =>
        new()
        {
            Success = false,
            Message = message,
            Data    = null
        };

    [Fact]
    public void Login_Get_WhenTokenExists_RedirectsToDashboard()
    {
        // Arrange
        _sut.ControllerContext = ControllerTestHelper.BuildControllerContext("existing-jwt-token");

        // Act
        var result = _sut.Login();

        // Assert
        result.Should().BeOfType<RedirectToActionResult>()
              .Which.ActionName.Should().Be("Index");
        result.As<RedirectToActionResult>().ControllerName.Should().Be("Dashboard");
    }

    [Fact]
    public void Login_Get_WhenNoToken_ReturnsViewWithModel()
    {
        // Arrange
        _sut.ControllerContext = ControllerTestHelper.BuildControllerContext((string?)null);

        // Act
        var result = _sut.Login();

        // Assert
        result.Should().BeOfType<ViewResult>()
              .Which.Model.Should().BeOfType<LoginRequestModel>();
    }

    [Fact]
    public async Task Login_Post_ValidAdminCredentials_SetsSessionAndReturnsOkTrue()
    {
        // Arrange
        _sut.ControllerContext = ControllerTestHelper.BuildControllerContext((string?)null);

        var urlHelperMock = new Mock<IUrlHelper>();
        urlHelperMock.Setup(u => u.Action(It.IsAny<UrlActionContext>()))
                     .Returns("/Dashboard/Index");
        _sut.Url = urlHelperMock.Object;

        var model = new LoginRequestModel { Username = "furkan", Password = "Passw0rd!" };
        _authApiClientMock
            .Setup(c => c.LoginAsync(model, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SuccessResult());

        // Act
        var result = await _sut.Login(model, CancellationToken.None);

        // Assert
        var json = result.Should().BeOfType<JsonResult>().Subject;
        var data = json.Value!;
        var okProp = data.GetType().GetProperty("ok")!.GetValue(data);
        okProp.Should().Be(true);

        var session = _sut.HttpContext.Session;
        session.GetString("token").Should().Be("jwt-token");
        session.GetString("username").Should().Be("furkan");
        session.GetString("role").Should().Be("Admin");
    }

    [Fact]
    public async Task Login_Post_WrongPassword_ReturnsJsonOkFalse()
    {
        // Arrange
        _sut.ControllerContext = ControllerTestHelper.BuildControllerContext((string?)null);

        var model = new LoginRequestModel { Username = "furkan", Password = "yanlisparola" };
        _authApiClientMock
            .Setup(c => c.LoginAsync(model, It.IsAny<CancellationToken>()))
            .ReturnsAsync(FailResult("Kullanıcı adı veya parola hatalı."));

        // Act
        var result = await _sut.Login(model, CancellationToken.None);

        // Assert
        var json = result.Should().BeOfType<JsonResult>().Subject;
        var ok = json.Value!.GetType().GetProperty("ok")!.GetValue(json.Value);
        ok.Should().Be(false);

        _sut.HttpContext.Session.GetString("token").Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task Login_Post_NonAdminRole_ReturnsJsonOkFalseWithRoleMessage()
    {
        // Arrange
        _sut.ControllerContext = ControllerTestHelper.BuildControllerContext((string?)null);

        var model = new LoginRequestModel { Username = "user1", Password = "pass" };
        _authApiClientMock
            .Setup(c => c.LoginAsync(model, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SuccessResult(role: "User"));

        // Act
        var result = await _sut.Login(model, CancellationToken.None);

        // Assert
        var json = result.Should().BeOfType<JsonResult>().Subject;
        var ok     = json.Value!.GetType().GetProperty("ok")!.GetValue(json.Value);
        var errors = json.Value!.GetType().GetProperty("errors")!.GetValue(json.Value) as IEnumerable<string>;

        ok.Should().Be(false);
        errors.Should().Contain(e => e.Contains("yönetici"));
        _sut.HttpContext.Session.GetString("token").Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task Login_Post_ApiUnreachable_ReturnsJsonOkFalse()
    {
        // Arrange
        _sut.ControllerContext = ControllerTestHelper.BuildControllerContext((string?)null);

        var model = new LoginRequestModel { Username = "furkan", Password = "pass" };
        var failResult = ApiResult<LoginResultModel>.Fail("API'ye erişilemedi. Lütfen sunucunun çalıştığından emin olun.", 503);
        _authApiClientMock
            .Setup(c => c.LoginAsync(model, It.IsAny<CancellationToken>()))
            .ReturnsAsync(failResult);

        // Act
        var result = await _sut.Login(model, CancellationToken.None);

        // Assert
        var json = result.Should().BeOfType<JsonResult>().Subject;
        var ok     = json.Value!.GetType().GetProperty("ok")!.GetValue(json.Value);
        var errors = json.Value!.GetType().GetProperty("errors")!.GetValue(json.Value) as IEnumerable<string>;

        ok.Should().Be(false);
        errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Login_Post_InvalidModelState_ReturnsJsonOkFalseWithValidationErrors()
    {
        // Arrange
        _sut.ControllerContext = ControllerTestHelper.BuildControllerContext((string?)null);
        _sut.ModelState.AddModelError("Username", "Kullanıcı adı zorunludur.");

        var model = new LoginRequestModel { Username = "", Password = "pass" };

        // Act
        var result = await _sut.Login(model, CancellationToken.None);

        // Assert
        var json = result.Should().BeOfType<JsonResult>().Subject;
        var ok     = json.Value!.GetType().GetProperty("ok")!.GetValue(json.Value);
        var errors = json.Value!.GetType().GetProperty("errors")!.GetValue(json.Value) as IEnumerable<string>;

        ok.Should().Be(false);
        errors.Should().Contain("Kullanıcı adı zorunludur.");
        _authApiClientMock.VerifyNoOtherCalls();
    }

    [Fact]
    public void Logout_Post_ClearsSessionAndRedirectsToLogin()
    {
        // Arrange
        _sut.ControllerContext = ControllerTestHelper.BuildControllerContext(
            new Dictionary<string, string>
            {
                { "token",    "some-jwt" },
                { "username", "furkan"   },
                { "role",     "Admin"    }
            });

        // Act
        var result = _sut.Logout();

        // Assert
        result.Should().BeOfType<RedirectToActionResult>()
              .Which.ActionName.Should().Be("Login");

        _sut.HttpContext.Session.GetString("token").Should().BeNullOrEmpty();
    }
}
