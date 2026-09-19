namespace FurkanTural_Business.Helpers;

public sealed class MailTally
{
    public int Sent { get; private set; }

    public int Retrying { get; private set; }

    public int Failed { get; private set; }

    public int Skipped { get; private set; }

    public string? LastError { get; private set; }

    public bool IsEmpty => Sent + Retrying + Failed + Skipped == 0;

    public bool HasFailures => Retrying + Failed > 0;

    public void RecordSent() => Sent++;

    public void RecordSkipped() => Skipped++;

    public void RecordFailure(string reason, bool final)
    {
        if (final)
            Failed++;
        else
            Retrying++;

        LastError = reason;
    }

    public string Summary()
    {
        var counts = $"Gönderilen: {Sent}, yeniden denenecek: {Retrying}, kalıcı başarısız: {Failed}, atlanan: {Skipped}.";
        return LastError is null ? counts : $"{counts} Son hata: {LastError.TrimEnd('.', ' ')}.";
    }
}
