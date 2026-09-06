namespace FurkanTural_Domain.Constants;

/// <summary><see cref="Entities.SubscriberVerification.Purpose"/> değerleri. Serbest metin değil sabit bir liste olmasının nedeni, jetonun amacının dışına kullanılamamasıdır: çıkış için üretilmiş bir bağlantı aboneliği onaylayamaz, onay bağlantısı da listeden düşüremez.</summary>
public static class SubscriberVerificationPurposes
{
    public const string Confirm = "Confirm";
    public const string Unsubscribe = "Unsubscribe";
    public const int MaxLength = 20;
}
