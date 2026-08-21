using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;

namespace EkimemoUtilities.Platforms.Android;

[Service(Exported = false, ForegroundServiceType = ForegroundService.TypeLocation)]
public class TimerForegroundService : Service
{
    public const string ActionStart = "dev.Ichihai1415.EkimemoUtilities.action.START_TIMER";
    public const string ActionStop = "dev.Ichihai1415.EkimemoUtilities.action.STOP_TIMER";
    public const string ExtraDurationSeconds = "duration_seconds";

    private const int NotificationId = 1001;
    private const string ChannelId = "timer_channel";

    private System.Threading.Timer? _timer;
    private int _remainingSeconds;

    // MainPage / オーバーレイ側が購読するための静的イベント
    public static event Action<int>? RemainingChanged;
    public static event Action? Completed;
    public static bool IsRunning { get; private set; }
    public static int RemainingSeconds { get; private set; }

    public override IBinder? OnBind(Intent? intent) => null;

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        if (intent?.Action == ActionStop)
        {
            StopTimer();
            return StartCommandResult.NotSticky;
        }

        var seconds = intent?.GetIntExtra(ExtraDurationSeconds, 0) ?? 0;
        if (seconds <= 0)
        {
            StopSelf();
            return StartCommandResult.NotSticky;
        }

        StartForegroundNotification();
        StartCountdown(seconds);

        return StartCommandResult.Sticky;
    }

    private void StartForegroundNotification()
    {
        EnsureChannel();

        var notification = new NotificationCompat.Builder(this, ChannelId)
            .SetContentTitle("タイマー動作中")
            .SetContentText("残り時間を計測しています")
            .SetSmallIcon(global::Android.Resource.Drawable.IcDialogInfo) // 暫定アイコン
            .SetOngoing(true)
            .Build();

        StartForeground(NotificationId, notification);
    }

    private void EnsureChannel()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O) return;

        var manager = (NotificationManager?)GetSystemService(NotificationService);
        if (manager?.GetNotificationChannel(ChannelId) == null)
        {
            var channel = new NotificationChannel(ChannelId, "タイマー通知", NotificationImportance.Low);
            manager?.CreateNotificationChannel(channel);
        }
    }

    private void StartCountdown(int seconds)
    {
        _remainingSeconds = seconds;
        RemainingSeconds = _remainingSeconds;
        IsRunning = true;
        RemainingChanged?.Invoke(_remainingSeconds);

        _timer?.Dispose();
        _timer = new System.Threading.Timer(OnTick, null, 1000, 1000);
    }

    private void OnTick(object? state)
    {
        _remainingSeconds--;
        RemainingSeconds = _remainingSeconds;
        RemainingChanged?.Invoke(_remainingSeconds);

        if (_remainingSeconds <= 0)
        {
            Vibrate();
            Completed?.Invoke();
            StopTimer();
        }
    }

    private void Vibrate()
    {
        var vibrator = (global::Android.OS.Vibrator?)GetSystemService(VibratorService);
        if (vibrator is null) return;

        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            var pattern = new long[] { 0, 500, 500, 500, 500, 2000 };
            var effect = VibrationEffect.CreateWaveform(pattern, -1);
            vibrator.Vibrate(effect);
        }
        else
        {
#pragma warning disable CA1422
            vibrator.Vibrate(3000);
#pragma warning restore CA1422
        }
    }

    private void StopTimer()
    {
        _timer?.Dispose();
        _timer = null;
        IsRunning = false;
        StopForeground(true);
        StopSelf();
    }

    public override void OnDestroy()
    {
        _timer?.Dispose();
        IsRunning = false;
        base.OnDestroy();
    }
}