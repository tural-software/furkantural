namespace FurkanTural_Domain.Constants;

/// <summary><see cref="Entities.AppSource"/> satırlarının Code anahtarları. Değerler yapılandırmadaki <c>AppTokens:AppName</c> girdileriyle ve dolayısıyla <c>app_source</c> claim'iyle harfi harfine aynıdır; bir istekten gelen kaynak adı bu sabitlerle karşılaştırılabilsin diye böyledir.<para>Admin'in app-token'ı yoktur, yani adı hiçbir claim'de geçmez; buradaki karşılığı yalnızca kendi şablonlarını sahiplenmesi içindir.</para></summary>
public static class AppSourceDefinitions
{
    public const string Portfolio = "Portfolio";
    public const string Blog = "Blog";
    public const string Chat = "Chat";
    public const string Admin = "Admin";
}
