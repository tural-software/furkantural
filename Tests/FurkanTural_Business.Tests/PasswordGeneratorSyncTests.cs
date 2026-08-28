using System.Text.RegularExpressions;
using FluentAssertions;
using FurkanTural_Business.Helpers;

namespace FurkanTural_Business.Tests;

public class PasswordGeneratorSyncTests
{
    private const string Gecerli = "Abc1!def";

    private const string SolutionMarker = "FurkanTural.slnx";

    private static string SolutionRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, SolutionMarker)))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException($"'{SolutionMarker}' bulunamadı.");
    }

    private static string Read(params string[] parts)
        => File.ReadAllText(Path.Combine([SolutionRoot(), .. parts]));

    private static readonly string[] Ureticiler =
    [
        Path.Combine("Presentation", "FurkanTural_Chat", "wwwroot", "js", "password-gen.js"),
        Path.Combine("Presentation", "FurkanTural_Admin", "wwwroot", "js", "password-gen.js"),
    ];

    [Fact]
    public void Uretici_scriptler_politikayla_ayni_sembol_kumesini_kullanir()
    {
        foreach (var yol in Ureticiler)
        {
            var kaynak = Read(yol.Split(Path.DirectorySeparatorChar));
            var eslesme = Regex.Match(kaynak, @"var SYMBOLS = '(?<kume>[^']*)';");

            eslesme.Success.Should().BeTrue($"{yol} içinde SYMBOLS tanımı bulunmalı");
            eslesme.Groups["kume"].Value.Should().Be(PasswordPolicy.Symbols,
                $"{yol} politikadan ayrışırsa üretilen parola sunucuda reddedilir");
        }
    }

    [Fact]
    public void Uretici_scriptler_politikanin_istedigi_uzunlugu_uretir()
    {
        foreach (var yol in Ureticiler)
        {
            var kaynak = Read(yol.Split(Path.DirectorySeparatorChar));
            var eslesme = Regex.Match(kaynak, @"var LENGTH = (?<uzunluk>\d+);");

            eslesme.Success.Should().BeTrue($"{yol} içinde LENGTH tanımı bulunmalı");
            int.Parse(eslesme.Groups["uzunluk"].Value).Should().Be(PasswordPolicy.GeneratedLength);
        }
    }

    private static Regex ChatDeseni()
    {
        var kaynak = Read("Presentation", "FurkanTural_Chat", "Models", "Auth", "RegisterRequestModel.cs");
        var eslesme = Regex.Match(kaynak, @"PasswordPattern\s*=\s*@""(?<desen>[^""]*)"";", RegexOptions.Singleline);

        eslesme.Success.Should().BeTrue("kayıt modelinde PasswordPattern sabiti bulunmalı");
        return new Regex(eslesme.Groups["desen"].Value);
    }

    [Fact]
    public void Chat_kayit_deseni_politikanin_kabul_ettigini_kabul_eder()
    {
        var desen = ChatDeseni();

        desen.IsMatch(Gecerli).Should().BeTrue();

        foreach (var sembol in PasswordPolicy.Symbols)
            desen.IsMatch($"Abc1de{sembol}").Should().BeTrue($"'{sembol}' sunucuda kabul ediliyor");
    }

    [Theory]
    [InlineData("Ab1!c")]
    [InlineData("abc1!def")]
    [InlineData("ABC1!DEF")]
    [InlineData("Abcd!efg")]
    [InlineData("Abc1defg")]
    [InlineData("Abc1!de\"")]
    [InlineData("Abc1!de ")]
    [InlineData("Abc1!deş")]
    public void Chat_kayit_deseni_politikanin_reddettigini_reddeder(string parola)
    {
        PasswordPolicy.Validate(parola).Should().NotBeNull();
        ChatDeseni().IsMatch(parola).Should().BeFalse();
    }
}
