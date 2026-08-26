namespace FurkanTural_Admin.Models.Navigation;

/// <summary>Bir kaydın bağlı olduğu alt modül. Detay çekmecesinin "İlişkili" sekmesi bu kayıttan beslenir; hedef adres ve süzgeç anahtarı listedeki geçiş düğmesiyle aynıdır, yani bağlantı zaten çalışan bir yere gider.</summary>
public sealed record AdminRelation(
    string ParentEntity,
    string ChildController,
    string FilterKey);
