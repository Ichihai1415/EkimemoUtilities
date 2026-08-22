using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using Android.Locations;
using AndroidX.Core.Content;
using EkimemoUtilities.Services;

namespace EkimemoUtilities.Platforms.Android;

[Service(Exported = false, ForegroundServiceType = ForegroundService.TypeLocation)]
public class TimerForegroundService : Service
{
    public const string ActionStart = "dev.Ichihai1415.EkimemoUtilities.action.START_TIMER";
    public const string ActionStop = "dev.Ichihai1415.EkimemoUtilities.action.STOP_TIMER";

    public const string ActionStartLocation = "dev.Ichihai1415.EkimemoUtilities.action.START_LOCATION";
    public const string ActionStopLocation = "dev.Ichihai1415.EkimemoUtilities.action.STOP_LOCATION";

    public static bool IsLocationRunning { get; private set; }

    private bool _isForegroundStarted;

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


    private LocationManager? _locationManager;
    private System.Threading.Timer? _locationTimer;

    public static event Action<global::Android.Locations.Location>? LocationChanged;
    public static global::Android.Locations.Location? LastLocation { get; private set; }


    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        switch (intent?.Action)
        {
            case ActionStop:
                StopTimer();
                break;

            case ActionStartLocation:
                StartForegroundIfNeeded();
                StartLocationUpdates();
                break;

            case ActionStopLocation:
                StopLocationUpdates();
                break;

            case ActionStart:
                var seconds = intent?.GetIntExtra(ExtraDurationSeconds, 0) ?? 0;
                if (seconds > 0)
                {
                    StartForegroundIfNeeded();
                    StartCountdown(seconds);
                }
                break;
        }

        return StartCommandResult.Sticky;
    }




    private void FetchLocationOnce()
    {
        if (ContextCompat.CheckSelfPermission(this, global::Android.Manifest.Permission.AccessFineLocation)
            != Permission.Granted)
        {
            return;   // 許可がなければ何もしない（クラッシュ防止）
        }

        _locationManager ??= (LocationManager?)GetSystemService(LocationService);
        if (_locationManager is null) return;

        var provider = _locationManager.IsProviderEnabled(LocationManager.GpsProvider)
            ? LocationManager.GpsProvider
            : LocationManager.NetworkProvider;

        if (!_locationManager.IsProviderEnabled(provider)) return;

        try
        {
            _locationManager.RequestSingleUpdate(
                provider,
                new SingleLocationListener(OnLocationReceived),
                Looper.MainLooper);
        }
        catch (Exception ex)
        {
            global::Android.Util.Log.Error("TimerForegroundService", $"位置情報取得失敗: {ex}");
        }
    }

    private void OnLocationReceived(global::Android.Locations.Location location)
    {
        LastLocation = location;
        LocationChanged?.Invoke(location);
        UpdateNotificationWithLocation(location);
    }

    private void UpdateNotificationWithLocation(global::Android.Locations.Location location)
    {
        var text = $"緯度:{location.Latitude:F5} 経度:{location.Longitude:F5}";

        var notification = new NotificationCompat.Builder(this, ChannelId)
            .SetContentTitle("タイマー動作中")
            .SetContentText(text)
            .SetSmallIcon(global::Android.Resource.Drawable.IcDialogInfo)
            .SetOngoing(true)
            .Build();

        NotificationManagerCompat.From(this).Notify(NotificationId, notification);
    }

    // 1回分の位置情報コールバックを受け取るための小さな内部クラス
    private class SingleLocationListener : Java.Lang.Object, ILocationListener
    {
        private readonly Action<global::Android.Locations.Location> _onLocation;
        public SingleLocationListener(Action<global::Android.Locations.Location> onLocation) => _onLocation = onLocation;

        public void OnLocationChanged(global::Android.Locations.Location location) => _onLocation(location);
        public void OnProviderDisabled(string provider) { }
        public void OnProviderEnabled(string provider) { }
        public void OnStatusChanged(string? provider, Availability status, Bundle? extras) { }
    }

    private void StartForegroundIfNeeded()
    {
        if (_isForegroundStarted) return;
        StartForegroundNotification();
        _isForegroundStarted = true;
    }
    private void StartLocationUpdates()
    {
        var intervalSeconds = Math.Max(5, EkimemoUtilities.Services.Settings.IntervalSeconds);
        IsLocationRunning = true;

        _locationTimer?.Dispose();
        _locationTimer = new System.Threading.Timer(_ => FetchLocationOnce(), null, 0, intervalSeconds * 1000);
    }

    private void StopLocationUpdates()
    {
        _locationTimer?.Dispose();
        _locationTimer = null;
        IsLocationRunning = false;

        MaybeStopServiceIfIdle();
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
        if (Settings.VibrationEnabled)
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
    }

    private void StopTimer()
    {
        _timer?.Dispose();
        _timer = null;
        IsRunning = false;

        MaybeStopServiceIfIdle();
    }

    private void MaybeStopServiceIfIdle()
    {
        if (!IsRunning && !IsLocationRunning)
        {
            StopForeground(true);
            _isForegroundStarted = false;
            StopSelf();
        }
    }

    public override void OnDestroy()
    {
        _timer?.Dispose();
        _locationTimer?.Dispose();
        IsRunning = false;
        IsLocationRunning = false;
        base.OnDestroy();
    }





}