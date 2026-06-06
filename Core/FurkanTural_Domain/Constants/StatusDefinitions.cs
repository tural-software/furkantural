namespace FurkanTural_Domain.Constants;

/// <summary>
/// <see cref="Entities.Status"/> tablosundaki statülerin sabit grup ve kod anahtarları.
/// Servisler statüyü id ile değil, bu sabitler (Group + Code) ile çözer.
/// </summary>
public static class StatusDefinitions
{
    public static class Groups
    {
        public const string Friendship = "Friendship";
    }

    public static class FriendshipCodes
    {
        public const string Pending = "Pending";
        public const string Accepted = "Accepted";
        public const string Rejected = "Rejected";
        public const string Blocked = "Blocked";
    }
}
