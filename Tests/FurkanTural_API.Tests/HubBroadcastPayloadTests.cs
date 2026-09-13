using FluentAssertions;
using FurkanTural_Application.DTOs.Common;

namespace FurkanTural_API.Tests;

public class HubBroadcastPayloadTests
{
    private static readonly Type[] AllowedPropertyTypes = [typeof(string), typeof(int), typeof(IReadOnlyList<AdminListChangeDto>)];

    [Theory]
    [InlineData(typeof(AdminListsChangedDto))]
    [InlineData(typeof(AdminListChangeDto))]
    [InlineData(typeof(AdminPendingWorkDto))]
    [InlineData(typeof(AdminSessionDto))]
    public void Yonetici_yayini_tur_adi_disinda_metin_tasimaz(Type payload)
    {
        var properties = payload.GetProperties();

        properties.Where(p => p.PropertyType == typeof(string) && p.Name != "Kind").Select(p => p.Name).Should().BeEmpty(
            "yayın bağlı her yöneticiye gider; mesaj metni, e-posta ya da kullanıcı adı taşımamalı, istemci ayrıntıyı yetkili uçtan kendisi çeker");

        properties.Where(p => !AllowedPropertyTypes.Contains(p.PropertyType)).Select(p => $"{p.Name}: {p.PropertyType.Name}").Should().BeEmpty(
            "yayın yalnızca tür, sayı ve kimlik taşır; yeni bir alan türü içerik sızdırmanın kapısını açar");
    }
}
