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

    public static bool VibrationEnabled
    {
        get => Preferences.Get("vibration_enabled", true);
        set => Preferences.Set("vibration_enabled", value);
    }

    public static bool NotificationEnabled
    {
        get => Preferences.Get("notification_enabled", true);
        set => Preferences.Set("notification_enabled", value);
    }

    public static bool HideLocationEnabled
    {
        get => Preferences.Get("hide_location_enabled", false);
        set => Preferences.Set("hide_location_enabled", value);
    }

    public static bool HideTimerEnabled
    {
        get => Preferences.Get("hide_timer_enabled", false);
        set => Preferences.Set("hide_timer_enabled", value);
    }

    public static double GPSWaitSeconds
    {
        get => Preferences.Get("gps_wait_seconds", 8d);
        set => Preferences.Set("gps_wait_seconds", value);
    }
}