namespace FurkanTural_Domain.Constants;

/// <summary>
/// Üyelik sözleşmesi / KVKK onayı için sabitler. Sözleşme metni güncellendiğinde
/// <see cref="CurrentVersion"/> artırılır; böylece mevcut üyeler yeniden onaylar.
/// </summary>
public static class AgreementDefinitions
{
    public const string CurrentVersion = "1.0";
}
