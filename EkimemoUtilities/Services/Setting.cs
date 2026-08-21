namespace EkimemoUtilities.Services;

public static class Settings
{
    private const string DurationSecondsKey = "timer_duration_seconds";
    private const int DefaultSeconds = 295;

    public static int DurationSeconds
    {
        get => Preferences.Get(DurationSecondsKey, DefaultSeconds);
        set => Preferences.Set(DurationSecondsKey, value);
    }
}