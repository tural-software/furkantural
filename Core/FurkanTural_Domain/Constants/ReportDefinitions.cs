namespace FurkanTural_Domain.Constants;

/// <summary>
/// <see cref="Entities.Report"/> için hedef-türü ve durum sabitleri. Şikayetin hangi tablonun
/// kaydına baktığını <see cref="TargetTypes"/> belirler, çünkü TargetId'nin foreign key'i yoktur.
/// </summary>
public static class ReportDefinitions
{
    public static class TargetTypes
    {
        public const string User = "User";
        public const string Message = "Message";
        public const string Media = "Media";
        public const string Call = "Call";
    }

    public static class Statuses
    {
        public const string Pending = "Pending";
        public const string Reviewed = "Reviewed";
        public const string Dismissed = "Dismissed";
        public const string ActionTaken = "ActionTaken";
    }

    public static bool IsValidTargetType(string? type) =>
        type is TargetTypes.User or TargetTypes.Message or TargetTypes.Media or TargetTypes.Call;

    public static bool IsValidStatus(string? status) =>
        status is Statuses.Pending or Statuses.Reviewed or Statuses.Dismissed or Statuses.ActionTaken;
}
