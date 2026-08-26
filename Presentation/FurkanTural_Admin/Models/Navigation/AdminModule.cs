using FurkanTural_Admin.Models.Dashboard;

namespace FurkanTural_Admin.Models.Navigation;

/// <summary>Bir yönetim modülünün gezinme kimliği: adı, grubu, controller'ı, izinleri ve sayım birimi. Ana ekran, modül seçici ve kırıntı yolu aynı kayıttan beslenir; ad bir yerde değişince her yerde değişir.</summary>
public sealed record AdminModule(
    string Slug,
    string ApiPath,
    string Entity,
    string Controller,
    string Title,
    string Description,
    string Group,
    IReadOnlyList<EntityAction> Actions,
    string CountUnitLabel,
    bool IsReady = true);
