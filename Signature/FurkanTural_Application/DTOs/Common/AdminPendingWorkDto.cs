namespace FurkanTural_Application.DTOs.Common;

public static class AdminWorkKinds
{
    public const string Comment = "comment";
    public const string Contact = "contact";
    public const string Report = "report";
}

public sealed record AdminPendingWorkDto(string Kind, int Comments, int Contacts, int Reports)
{
    public int Total => Comments + Contacts + Reports;
}
