namespace EkimemoUtilities.Services;

public static class Settings
{
    public static int DurationSeconds
    {
        get => Preferences.Get("timer_duration_seconds", 295);
        set => Preferences.Set("timer_duration_seconds", value);
    }

    public static int IntervalSeconds
    {
        get => Preferences.Get("location_interval_seconds", 5);
        set => Preferences.Set("location_interval_seconds", value);
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

    public static int NearSt_Distance
    {
        get => Preferences.Get("near_st_distance", 5);
        set => Preferences.Set("near_st_distance", value);
    }

    public static int NearSt_MaxCount
    {
        get => Preferences.Get("near_st_maxcount", 3);
        set => Preferences.Set("near_st_maxcount", value);
    }

    public static bool ResetTimer_OnlyGPS
    {
        get => Preferences.Get("reset_timer_only_gps", true);
        set => Preferences.Set("reset_timer_only_gps", value);
    }

}