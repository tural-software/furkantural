namespace FurkanTural_Domain.Constants;

/// <summary><see cref="Entities.Status"/> satırlarının Group + Code anahtarları; servisler statüyü Id ile değil bu ikiliyle çözer.</summary>
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
