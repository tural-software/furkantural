namespace FurkanTural_Chat.Models.Auth;

/// <summary>Doğrulama bağlantısının karşılama sayfası. Jeton bağlantıdan gelir ve onay adımına gizli alan olarak taşınır; sayfayı açmak hesabı açmaz.<para>Ayrım bilerek yapılmıştır: bağlantıyı yalnızca istemek jetonu harcamamalıdır. Kurumsal posta tarayıcıları ve tarayıcı ön-yüklemeleri gelen bağlantıları kendiliğinden ister, dolayısıyla GET üzerinde etkinleştirme kullanıcı henüz tıklamadan jetonu tüketebilirdi.</para></summary>
public class ActivateAccountModel
{
    public string? Token { get; set; }
    public ActivationState State { get; set; } = ActivationState.Confirm;
    public string? Message { get; set; }
}

/// <summary>Karşılama sayfasının gösterdiği durum. Başarısızlıkta ayrıca <c>Expired</c> gibi alt durumlar tutulmaz: hangi sebeple reddedildiği kullanıcıya API'nin mesajıyla anlatılır, sayfanın sunacağı çıkış yolu her hâlükârda aynıdır.</summary>
public enum ActivationState
{
    Confirm,
    Success,
    Failed,
    MissingToken
}
