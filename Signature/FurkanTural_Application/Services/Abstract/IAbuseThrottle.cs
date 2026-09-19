namespace FurkanTural_Application.Services.Abstract;

public static class AbuseBuckets
{
    public const string Contact = "Contact";
    public const string Newsletter = "Newsletter";
    public const string Report = "Report";
    public const string FriendRequest = "FriendRequest";
    public const string CallConfig = "CallConfig";
}

public interface IAbuseThrottle
{
    /// <summary>Anahtar boşsa sınır uygulanmaz: çağıran taraf IP'yi çözemediğinde isteği reddetmek, kimliği belirsiz diye herkesi kapı dışında bırakmak olurdu. Eşikler <c>Abuse:&lt;kova&gt;</c> altından okunur, yoksa koddaki varsayılana düşülür.</summary>
    bool TryRegister(string bucket, string? key);
}
