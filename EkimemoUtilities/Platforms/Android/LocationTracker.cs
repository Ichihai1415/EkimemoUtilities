using Android.Content;
using Android.OS;
using EkimemoUtilities.Platforms.Android;
using EkimemoUtilities.Services;
using AndroidApplication = Android.App.Application;

namespace EkimemoUtilities.Platforms.Android;

public class LocationTracker : ILocationTracker
{
    public bool IsRunning => TimerForegroundService.IsLocationRunning;
    public LocationInfo? Last { get; private set; }

    public event Action<LocationInfo>? LocationChanged;

    public LocationTracker()
    {
        TimerForegroundService.LocationChanged += OnNativeLocationChanged;
    }

    private void OnNativeLocationChanged(global::Android.Locations.Location location)
    {
        var info = new LocationInfo(
            Latitude: location.Latitude,
            Longitude: location.Longitude,
            Altitude: location.HasAltitude ? location.Altitude : null,
            AccuracyMeters: location.HasAccuracy ? location.Accuracy : null,
            TimestampUtc: DateTime.UtcNow);

        Last = info;
        LocationChanged?.Invoke(info);
    }

    public void Start()
    {
        var context = AndroidApplication.Context;
        var intent = new Intent(context, typeof(TimerForegroundService));
        intent.SetAction(TimerForegroundService.ActionStartLocation);

        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            context.StartForegroundService(intent);
        else
            context.StartService(intent);
    }

    public void Stop()
    {
        var context = AndroidApplication.Context;
        var intent = new Intent(context, typeof(TimerForegroundService));
        intent.SetAction(TimerForegroundService.ActionStopLocation);
        context.StartService(intent);
    }
}