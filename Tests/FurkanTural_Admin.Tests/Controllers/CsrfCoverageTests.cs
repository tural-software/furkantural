using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_Admin.Tests.Controllers;

/// <summary>
/// [HttpPost] attribute taşıyan tüm public action'ların
/// [ValidateAntiForgeryToken] attribute de taşıdığını doğrular.
/// Test FAIL bırakılmaz; eksik olanlar listelenip rapor çıkarılır.
/// NOT: Güvenlik bulgusu varsa testi dikkat çekmek için fail yapılır.
/// </summary>
public class CsrfCoverageTests
{
    private static readonly Assembly AdminAssembly =
        typeof(FurkanTural_Admin.Controllers.AuthController).Assembly;

    // AuthController ve HomeController zaten kuralı uyguluyor — hepsini tarıyoruz.
    private static readonly Type ControllerBaseType = typeof(ControllerBase);

    private static IEnumerable<(Type Controller, MethodInfo Method)> GetAllHttpPostActions()
    {
        var controllers = AdminAssembly.GetTypes()
            .Where(t => !t.IsAbstract && t.IsPublic && t.IsAssignableTo(ControllerBaseType));

        foreach (var ctrl in controllers)
        {
            var methods = ctrl.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => m.GetCustomAttribute<HttpPostAttribute>() != null);

            foreach (var method in methods)
                yield return (ctrl, method);
        }
    }

    [Fact]
    public void AllHttpPostActions_ShouldHave_ValidateAntiForgeryToken()
    {
        // Arrange
        var postActions = GetAllHttpPostActions().ToList();

        postActions.Should().NotBeEmpty("Admin panelinde HttpPost action bulunmalıdır.");

        // Act
        var missingCsrf = postActions
            .Where(pair => pair.Method.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>() == null)
            .Select(pair => $"{pair.Controller.Name}.{pair.Method.Name}")
            .ToList();

        // Assert
        // Test, eksik CSRF action varsa fail eder — bu kasıtlı bir güvenlik bulgusudur.
        missingCsrf.Should().BeEmpty(
            $"Aşağıdaki [HttpPost] action'larda [ValidateAntiForgeryToken] EKSIK (CSRF riski):{Environment.NewLine}" +
            string.Join(Environment.NewLine, missingCsrf));
    }

    [Fact]
    public void HttpPostActionCount_ShouldBe_GreaterThanZero()
    {
        // Arrange & Act
        var count = GetAllHttpPostActions().Count();

        // Assert — assembly'nin doğru yüklendiğini doğrular
        count.Should().BeGreaterThan(0, "Admin panelinde en az bir HttpPost action olmalıdır.");
    }
}
