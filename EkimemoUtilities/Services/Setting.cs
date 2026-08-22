namespace EkimemoUtilities.Services;

public static class Settings
{
    private const string DurationSecondsKey = "timer_duration_seconds";
    private const int DS_DefaultSeconds = 295;

    public static int DurationSeconds
    {
        get => Preferences.Get(DurationSecondsKey, DS_DefaultSeconds);
        set => Preferences.Set(DurationSecondsKey, value);
    }

    private const string IntervalSecondsKey = "location_interval_seconds";
    private const int II_DefaultSeconds = 5;

    public static int IntervalSeconds
    {
        get => Preferences.Get(IntervalSecondsKey, II_DefaultSeconds);
        set => Preferences.Set(IntervalSecondsKey, value);
    }
}