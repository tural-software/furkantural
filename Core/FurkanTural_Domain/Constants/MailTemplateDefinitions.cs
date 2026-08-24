namespace FurkanTural_Domain.Constants;

/// <summary><see cref="Entities.MailTemplateType"/> satırlarının Code anahtarları. Servisler şablonu Id ile değil bu kodlarla çözer, çünkü Id yalnızca tohum satırlarında sabittir; panelden eklenen türler için sabit yoktur.<para>Buradaki her kodun karşılığında bir posta DTO'su ve o DTO'yu dolduran bir çağıran vardır. Panelden eklenen bir tür bu listeye girmez: şablonu saklanır ama onu gönderen kod olmadığı için kendiliğinden postaya dönüşmez.</para></summary>
public static class MailTemplateDefinitions
{
    public const string ContactOwner = "ContactOwner";
    public const string ContactUser = "ContactUser";
    public const string AccountActivation = "AccountActivation";
}
