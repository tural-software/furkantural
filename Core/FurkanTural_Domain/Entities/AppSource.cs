using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Domain.Entities;

/// <summary>Çözümdeki sunum projelerinin sözlüğü. Code, giriş sırasında JWT'ye yazılan <c>app_source</c> claim'i ve yapılandırmadaki <c>AppTokens:AppName</c> ile aynı değerdir; ayrım bugüne kadar yalnızca o iki yerde, yani veri tabanının dışında yaşıyordu.<para><see cref="Project"/> ile karıştırılmamalı: o, portfolyo vitrinindeki proje kartıdır. Buradaki kayıt bir uygulamayı temsil eder.</para><para>Bir satırın karşılığı yalnızca <see cref="Constants.AppSourceDefinitions"/> sabitleri kadar vardır. Panelden yeni satır eklenebilir, ama o adı taşıyan bir ön-yüz ve ona app-token veren bir yapılandırma yoksa satır tek başına bir şey yapmaz.</para></summary>
public class AppSource : BaseEntity
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
}
