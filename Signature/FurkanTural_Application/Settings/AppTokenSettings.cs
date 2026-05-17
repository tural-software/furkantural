namespace FurkanTural_Application.Settings;

public class AppTokenSettings
{
    public int ExpiryDays { get; set; } = 365;
    public List<AppRegistration> Apps { get; set; } = [];
}

public class AppRegistration
{
    public string AppName { get; set; } = string.Empty;
    public string AppKey { get; set; } = string.Empty;
}
