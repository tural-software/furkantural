namespace FurkanTural_Domain.Constants;

/// <summary>Yürürlükteki üyelik sözleşmesi sürümü. Kayıtta <see cref="Entities.User.MembershipAgreementVersion"/> alanına yazılır ve sonraki girişlerde buradaki değerle karşılaştırılır; metin değişip sabit ilerletildiğinde eşleşme bozulur ve mevcut üyelerden yeniden onay istenir.</summary>
public static class AgreementDefinitions
{
    public const string CurrentVersion = "1.0";
}
