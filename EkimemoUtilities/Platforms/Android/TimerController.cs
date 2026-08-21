using Android.Content;
using Android.OS;
using EkimemoUtilities.Platforms.Android;
using EkimemoUtilities.Services;
using AndroidApplication = Android.App.Application;

namespace EkimemoUtilities.Platforms.Android;

public class TimerController : ITimerController
{
    public bool IsRunning => TimerForegroundService.IsRunning;
    public TimeSpan Remaining => TimeSpan.FromSeconds(TimerForegroundService.RemainingSeconds);

    public event Action<TimeSpan>? RemainingChanged;
    public event Action? Completed;

    public TimerController()
    {
        TimerForegroundService.RemainingChanged += s => RemainingChanged?.Invoke(TimeSpan.FromSeconds(s));
        TimerForegroundService.Completed += () => Completed?.Invoke();
    }

    public void Start(TimeSpan duration)
    {
        var context = AndroidApplication.Context;
        var intent = new Intent(context, typeof(TimerForegroundService));
        intent.SetAction(TimerForegroundService.ActionStart);
        intent.PutExtra(TimerForegroundService.ExtraDurationSeconds, (int)duration.TotalSeconds);

        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            context.StartForegroundService(intent);
        else
            context.StartService(intent);
    }

    public void Stop()
    {
        var context = AndroidApplication.Context;
        var intent = new Intent(context, typeof(TimerForegroundService));
        intent.SetAction(TimerForegroundService.ActionStop);
        context.StartService(intent);
    }
}