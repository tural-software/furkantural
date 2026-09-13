namespace FurkanTural_Application.DTOs.Common;

public static class AdminListKinds
{
    public const string Comment = AdminWorkKinds.Comment;
    public const string Contact = AdminWorkKinds.Contact;
    public const string Report = AdminWorkKinds.Report;
    public const string User = "user";
    public const string Friend = "friend";
    public const string Message = "message";
    public const string Call = "call";
    public const string Subscriber = "subscriber";
    public const string Newsletter = "newsletter";

    public static readonly IReadOnlyList<string> All = [Comment, Contact, Report, User, Friend, Message, Call, Subscriber, Newsletter];

    public static readonly IReadOnlyList<string> Work = [Comment, Contact, Report];
}
