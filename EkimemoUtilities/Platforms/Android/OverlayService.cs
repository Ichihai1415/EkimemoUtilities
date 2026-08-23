using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.Core.App;
using EkimemoUtilities.Services;
using Microsoft.Maui.Controls.Platform;


namespace EkimemoUtilities.Platforms.Android;

public class OverlayService : IOverlayService
{
    private readonly ITimerController _timerController;
    private readonly ILocationTracker _locationTracker;

    public OverlayService(ITimerController timerController, ILocationTracker locationTracker)
    {
        _timerController = timerController;
        _locationTracker = locationTracker;
    }

    private global::Android.Views.IWindowManager? _windowManager;
    private global::Android.Views.View? _overlayView;
    private global::Android.Widget.TextView? _timeLabel;
    private readonly Handler _mainHandler = new(Looper.MainLooper!);

    private global::Android.Views.WindowManagerLayoutParams? _layoutParams;
    private int _initialX, _initialY;
    private float _initialTouchX, _initialTouchY;

    public bool IsShowing { get; private set; }

    public bool HasPermission()
        => global::Android.Provider.Settings.CanDrawOverlays(global::Android.App.Application.Context);

    public void RequestPermission()
    {
        var intent = new Intent(
            global::Android.Provider.Settings.ActionManageOverlayPermission,
            global::Android.Net.Uri.Parse($"package:{(global::Android.App.Application.Context.PackageName)}"));
        intent.SetFlags(ActivityFlags.NewTask);
        global::Android.App.Application.Context.StartActivity(intent);
    }

    TextView? _div;
    TextView? _div2;
    LinearLayout? _buttonRow;

    public void Show()
    {
        if (IsShowing) return;

        if (!HasPermission())
        {
            RequestPermission();
            return;
        }

        try
        {
            var context = global::Android.App.Application.Context;
            var themedContext = new global::Android.Views.ContextThemeWrapper(
                context, global::Android.Resource.Style.ThemeMaterialLight);

            var raw = context.GetSystemService(global::Android.Content.Context.WindowService);
            _windowManager = raw?.JavaCast<global::Android.Views.IWindowManager>();

            // 全体を縦に並べるコンテナ
            var container = new global::Android.Widget.LinearLayout(themedContext)
            {
                Orientation = global::Android.Widget.Orientation.Vertical
            };

            var density = context.Resources!.DisplayMetrics!.Density;
            var background = new GradientDrawable();
            background.SetShape(ShapeType.Rectangle);
            background.SetCornerRadius(16f * density);//16dpを実ピクセルに変換
            background.SetColor(global::Android.Graphics.Color.Argb(192, 30, 60, 90));
            container.SetBackground(background);



            container.SetPadding(24, 16, 24, 16);

            var headRow = new global::Android.Widget.LinearLayout(themedContext)
            {
                Orientation = global::Android.Widget.Orientation.Horizontal
            };

            var threeLine = new global::Android.Widget.TextView(themedContext)
            {
                Text = "≡ ",
                TextSize = 20
            };


            threeLine.SetTextColor(global::Android.Graphics.Color.White);
            threeLine.Gravity = global::Android.Views.GravityFlags.Left;

            //threeLine.Click += (s, e) => OpenMainApp();
            threeLine.Touch += Move;


            var dragHandle = new global::Android.Widget.TextView(themedContext)
            {
                Text = "駅メモUtilities",
                TextSize = 20
            };
            dragHandle.SetTextColor(global::Android.Graphics.Color.LightGray);
            dragHandle.Gravity = global::Android.Views.GravityFlags.Left;

            dragHandle.Click += (s, e) => ToggleMinView();
            dragHandle.LongClick += (s, e) => OpenMainApp();

            headRow.AddView(threeLine);
            headRow.AddView(dragHandle);
            container.AddView(headRow);

            /*
            var divider = new global::Android.Views.View(themedContext);
            divider.SetBackgroundColor(global::Android.Graphics.Color.Argb(127, 255, 255, 255));
            var dividerParams = new global::Android.Widget.LinearLayout.LayoutParams(
                400, (int)(1 * density));
            dividerParams.SetMargins(0, 8, 0, 8);
            divider.LayoutParameters = dividerParams;
            container.AddView(divider);
            */

            _div = new global::Android.Widget.TextView(themedContext)
            {
                Text = "----------------------------------------",
                TextSize = 10
            };
            _div.SetTextColor(global::Android.Graphics.Color.LightGray);
            _div.Gravity = global::Android.Views.GravityFlags.CenterHorizontal;

            container.AddView(_div);


            _stationLabel = new TextView(themedContext)
            {
                Text = GetStation(_locationTracker.Last),
                TextSize = 20
            };
            _stationLabel.SetTextColor(global::Android.Graphics.Color.White);
            _stationLabel.Gravity = GravityFlags.CenterHorizontal;
            if (_stationLabel.Text == "" || _isMinimized)
                _stationLabel.Visibility = ViewStates.Gone;
            else
                _stationLabel.Visibility = ViewStates.Visible;
            container.AddView(_stationLabel);


            _locationLabel = new TextView(themedContext)
            {
                Text = FormatLocation(_locationTracker.Last),
                TextSize = 12
            };
            _locationLabel.SetTextColor(global::Android.Graphics.Color.White);
            _locationLabel.Gravity = GravityFlags.CenterHorizontal;

            container.AddView(_locationLabel);


            _div2 = new global::Android.Widget.TextView(themedContext)
            {
                Text = "----------------------------------------",
                TextSize = 10
            };
            _div2.SetTextColor(global::Android.Graphics.Color.LightGray);
            _div2.Gravity = global::Android.Views.GravityFlags.CenterHorizontal;

            container.AddView(_div2);


            // 残り時間表示
            _timeLabel = new global::Android.Widget.TextView(themedContext)
            {
                Text = FormatTime(TimerForegroundService.RemainingSeconds),
                TextSize = 36
            };
            _timeLabel.SetTextColor(global::Android.Graphics.Color.White);
            _timeLabel.Gravity = global::Android.Views.GravityFlags.CenterHorizontal;

            // ボタンを横並びにする行
            _buttonRow = new global::Android.Widget.LinearLayout(themedContext)
            {
                Orientation = global::Android.Widget.Orientation.Horizontal
            };

            var startButton = new global::Android.Widget.Button(themedContext) { Text = "(RE)START" };
            startButton.SetTextColor(global::Android.Graphics.Color.White);
            startButton.SetBackgroundColor(global::Android.Graphics.Color.Argb(127, 0, 30, 60));
            startButton.Click += (s, e) => _timerController.Start(TimeSpan.FromSeconds(Settings.DurationSeconds));


            var stopButton = new global::Android.Widget.Button(themedContext) { Text = "RESET" };
            stopButton.SetTextColor(global::Android.Graphics.Color.White);
            stopButton.SetBackgroundColor(global::Android.Graphics.Color.Argb(127, 0, 30, 60));
            stopButton.Click += (s, e) =>
            {
                _timerController.Stop();
                _timeLabel?.Text = FormatTime(0);
            };

            var buttonParams = new global::Android.Widget.LinearLayout.LayoutParams(
                global::Android.Views.ViewGroup.LayoutParams.WrapContent, global::Android.Views.ViewGroup.LayoutParams.WrapContent);
            buttonParams.SetMargins(4, 0, 4, 20);


            _buttonRow.AddView(startButton, buttonParams);
            _buttonRow.AddView(stopButton, buttonParams);

            container.AddView(_timeLabel);
            container.AddView(_buttonRow);

            //var openAppButton = new global::Android.Widget.Button(themedContext) { Text = "Open Application", TextSize = 12 };
            //openAppButton.Click += (s, e) => OpenMainApp();
            //openAppButton.SetTextColor(global::Android.Graphics.Color.White);
            //openAppButton.SetBackgroundColor(global::Android.Graphics.Color.Argb(127, 127, 159, 191));
            //
            //container.AddView(openAppButton);


            _overlayView = container;

            var overlayType = Build.VERSION.SdkInt >= BuildVersionCodes.O
                ? global::Android.Views.WindowManagerTypes.ApplicationOverlay
                : global::Android.Views.WindowManagerTypes.Phone;

            var layoutParams = new global::Android.Views.WindowManagerLayoutParams(
                global::Android.Views.WindowManagerLayoutParams.WrapContent,
                global::Android.Views.WindowManagerLayoutParams.WrapContent,
                overlayType,
                global::Android.Views.WindowManagerFlags.NotFocusable | WindowManagerFlags.LayoutNoLimits,
                global::Android.Graphics.Format.Translucent)
            {
                Gravity = global::Android.Views.GravityFlags.Top | global::Android.Views.GravityFlags.Left,
                X = 100,
                Y = 100
            };
            _layoutParams = layoutParams;
            _windowManager?.AddView(_overlayView, layoutParams);





            IsShowing = true;


            TimerForegroundService.RemainingChanged += OnRemainingChanged;
            _locationTracker.LocationChanged += OnLocationChanged;
            _locationTracker.RunningChanged += OnLocationRunningChanged;
        }
        catch (Exception ex)
        {
            global::Android.Util.Log.Error("OverlayService", $"Display failed: {ex}");
        }
    }

    private void Move(object? s, global::Android.Views.View.TouchEventArgs e)
    {
        if (_layoutParams is null || _windowManager is null || _overlayView is null)
            return;

        switch (e.Event!.Action)
        {
            case global::Android.Views.MotionEventActions.Down:
                _initialX = _layoutParams.X;
                _initialY = _layoutParams.Y;
                _initialTouchX = e.Event.RawX;
                _initialTouchY = e.Event.RawY;
                e.Handled = true;
                break;

            case global::Android.Views.MotionEventActions.Move:
                _layoutParams.X = _initialX + (int)(e.Event.RawX - _initialTouchX);
                _layoutParams.Y = _initialY + (int)(e.Event.RawY - _initialTouchY);
                _windowManager.UpdateViewLayout(_overlayView, _layoutParams);
                e.Handled = true;
                break;

            case global::Android.Views.MotionEventActions.Up:
                e.Handled = true;
                break;
        }
    }

    public void Hide()
    {
        if (!IsShowing || _overlayView is null) return;

        _windowManager?.RemoveView(_overlayView);

        TimerForegroundService.RemainingChanged -= OnRemainingChanged;
        _locationTracker.LocationChanged -= OnLocationChanged;
        _locationTracker.RunningChanged -= OnLocationRunningChanged;
        _overlayView = null;
        IsShowing = false;
    }

    private void OnLocationRunningChanged(bool isRunning)
    {
        _stationLabel?.Visibility = ViewStates.Gone;
        _locationLabel?.Text = isRunning ? "Getting start. Please wait..." : "Getting location is off.";
    }

    public void Toggle()
    {
        global::Android.Util.Log.Debug("OverlayService", $"Toggle called. IsShowing={IsShowing}");
        if (IsShowing) Hide();
        else Show();
    }

    private void OnRemainingChanged(int seconds)
    {
        _mainHandler.Post(() =>
        {
            _timeLabel?.Text = FormatTime(seconds);
        });
    }

    private static string FormatTime(int totalSeconds)
    {
        if (totalSeconds < 0) totalSeconds = 0;
        var span = TimeSpan.FromSeconds(totalSeconds);
        return span.ToString(@"mm\:ss");
    }

    private TextView? _locationLabel;
    private TextView? _stationLabel;

    private void OnLocationChanged(LocationInfo location)
    {
        _locationLabel?.Text = FormatLocation(location);
        _stationLabel?.Text = GetStation(location);
        if (_stationLabel?.Text == "" || _isMinimized)
            _stationLabel?.Visibility = ViewStates.Gone;
        else
            _stationLabel?.Visibility = ViewStates.Visible;
    }

    private string FormatLocation(LocationInfo? info)
    {
        if (!_locationTracker.IsRunning) return "Getting location is off.";
        if (info is null) return "Waiting for location...";

        var altitudeText = info.Value.Altitude is double alt
            ? $"H: {alt:F1}m"
            : "H: ---m";

        var accuracyText = info.Value.AccuracyMeters is double acc
            ? $"A: {acc:F1}m"
            : "A: ---m";

        var speedText = info.Value.Speed is double spd
            ? $"S: {(spd * 3.6):F1}km/h"
            : "S: ---km/h";
        return $"{info.Value.Latitude:F6}, {info.Value.Longitude:F6}\n{altitudeText} {accuracyText} {speedText}";
    }

    internal string lastStationName = "";

    public string GetStation(LocationInfo? info)
    {
        if (MauiProgram.geojson is null) return "No station data!";
        if (!_locationTracker.IsRunning) return "";
        if (info is null) return "";

        var stationName = MauiProgram.geojson.FindName(info.Value.Latitude, info.Value.Longitude);
        if (stationName != null)
        {
            //debug
            //ShowNotification_Station("チェックインしよう！", stationName + "駅エリアに入りました");

            if (lastStationName != "" && lastStationName != stationName)
            {
                ShowNotification_Station("チェックインしよう！", stationName + "駅エリアに入りました。ここをタップで駅メモを開きます。");
                //ShowNotification(TimerForegroundService.ChannelId_Station, TimerForegroundService.NotificationId_Station, "チェックインしよう！", stationName + "駅エリアに入りました", global::Android.Resource.Drawable.IcDialogInfo);

                Vibrate();
                if (_timerController.IsRunning)
                {
                    _timerController.Stop();
                    _timeLabel?.Text = FormatTime(0);
                }
            }
            lastStationName = stationName!;
        }
        return stationName ?? "(No matching station)";
    }

    private void Vibrate()
    {
        if (Settings.VibrationEnabled)
        {
            var context = global::Android.App.Application.Context;
            var vibrator = (global::Android.OS.Vibrator?)context.GetSystemService(Context.VibratorService);
            if (vibrator is null) return;
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var pattern = new long[] { 0, 100, 100, 100, 100, 750, 250, 100, 100, 100, 100, 750 };
                var effect = VibrationEffect.CreateWaveform(pattern, -1);
                vibrator.Vibrate(effect);
            }
            else
            {
#pragma warning disable CA1422
                vibrator.Vibrate(1000);
#pragma warning restore CA1422
            }
        }
    }
    private void OpenMainApp()
    {
        var context = global::Android.App.Application.Context;
        var packageManager = context.PackageManager;

        var launchIntent = packageManager?.GetLaunchIntentForPackage(context.PackageName!);
        if (launchIntent is null)
        {
            global::Android.Util.Log.Error("OverlayService", "起動用Intentの取得に失敗");
            return;
        }

        launchIntent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ReorderToFront);
        context.StartActivity(launchIntent);
    }

    private void ShowNotification_Station(string title, string text)
    {
        if (Settings.NotificationEnabled)
        {
            var context = global::Android.App.Application.Context;

            var notification = new NotificationCompat.Builder(context, TimerForegroundService.ChannelId_Station)
                .SetContentTitle(title)?
                .SetContentText(text)?
                .SetSmallIcon(global::Android.Resource.Drawable.IcDialogMap)?
                .SetContentIntent(CreateOpenAppPendingIntent("jp.mfapps.loc.ekimemo"))?
                .SetAutoCancel(true)?
                .Build();

            NotificationManagerCompat.From(context)?.Notify(TimerForegroundService.NotificationId_Station, notification);
        }
    }


    internal PendingIntent? CreateOpenAppPendingIntent(string packageName)
    {
        var context = global::Android.App.Application.Context;
        var packageManager = context.PackageManager;

        var launchIntent = packageManager?.GetLaunchIntentForPackage(packageName);
        if (launchIntent is null) return null;

        launchIntent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ReorderToFront);

        var flags = Build.VERSION.SdkInt >= BuildVersionCodes.S
            ? PendingIntentFlags.Immutable
            : PendingIntentFlags.UpdateCurrent;

        return PendingIntent.GetActivity(context, 0, launchIntent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
    }



    private bool _isMinimized = false;
    private void ToggleMinView()
    {
        _div?.Visibility = _isMinimized ? ViewStates.Visible : ViewStates.Gone;
        _stationLabel?.Visibility = _isMinimized && _stationLabel?.Text != "" ? ViewStates.Visible : ViewStates.Gone;
        _locationLabel?.Visibility = _isMinimized ? ViewStates.Visible : ViewStates.Gone;
        _div2?.Visibility = _isMinimized ? ViewStates.Visible : ViewStates.Gone;
        _timeLabel?.Visibility = _isMinimized ? ViewStates.Visible : ViewStates.Gone;
        _buttonRow?.Visibility = _isMinimized ? ViewStates.Visible : ViewStates.Gone;

        _isMinimized = !_isMinimized;
    }

}