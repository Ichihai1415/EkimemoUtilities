using Android.Content;
using Android.OS;
using EkimemoUtilities.Services;
using AndroidApplication = Android.App.Application;

namespace EkimemoUtilities.Platforms.Android;

public class LocationTracker : ILocationTracker
{
    public bool IsRunning => TimerForegroundService.IsLocationRunning;
    public LocationInfo? Last { get; private set; }

    public event Action<LocationInfo>? LocationChanged;
    public event Action<bool>? RunningChanged;
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
            Speed: location.HasSpeed ? location.Speed : 0,
            AccuracyMeters: location.HasAccuracy ? location.Accuracy : null,
            Timestamp: DateTime.Now);

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

        RunningChanged?.Invoke(true);
    }

    public void Stop()
    {
        var context = AndroidApplication.Context;
        var intent = new Intent(context, typeof(TimerForegroundService));
        intent.SetAction(TimerForegroundService.ActionStopLocation);
        context.StartService(intent);

        RunningChanged?.Invoke(false);
    }
}