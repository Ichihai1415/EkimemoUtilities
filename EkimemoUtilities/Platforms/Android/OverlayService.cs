using Android.Content;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
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

            var dragHandle = new global::Android.Widget.TextView(themedContext)
            {
                Text = "≡ 駅メモUtilities",
                TextSize = 20
            };
            dragHandle.SetTextColor(global::Android.Graphics.Color.LightGray);
            dragHandle.Gravity = global::Android.Views.GravityFlags.Left;

            dragHandle.Touch += (s, e) =>
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
                        _layoutParams.Y = _initialY - (int)(e.Event.RawY - _initialTouchY);
                        _windowManager.UpdateViewLayout(_overlayView, _layoutParams);
                        e.Handled = true;
                        break;

                    case global::Android.Views.MotionEventActions.Up:
                        e.Handled = true;
                        break;
                }
            };
            container.AddView(dragHandle);

            /*
            var divider = new global::Android.Views.View(themedContext);
            divider.SetBackgroundColor(global::Android.Graphics.Color.Argb(127, 255, 255, 255));
            var dividerParams = new global::Android.Widget.LinearLayout.LayoutParams(
                400, (int)(1 * density));
            dividerParams.SetMargins(0, 8, 0, 8);
            divider.LayoutParameters = dividerParams;
            container.AddView(divider);
            */

            var div = new global::Android.Widget.TextView(themedContext)
            {
                Text = "----------------------------------------",
                TextSize = 10
            };
            div.SetTextColor(global::Android.Graphics.Color.LightGray);
            div.Gravity = global::Android.Views.GravityFlags.CenterHorizontal;

            container.AddView(div);


            _stationLabel = new TextView(themedContext)
            {
                Text = GetStation(_locationTracker.Last),
                TextSize = 20
            };
            _stationLabel.SetTextColor(global::Android.Graphics.Color.White);
            _stationLabel.Gravity = GravityFlags.CenterHorizontal;
            if (_stationLabel.Text == "")
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


            var div2 = new global::Android.Widget.TextView(themedContext)
            {
                Text = "----------------------------------------",
                TextSize = 10
            };
            div2.SetTextColor(global::Android.Graphics.Color.LightGray);
            div2.Gravity = global::Android.Views.GravityFlags.CenterHorizontal;

            container.AddView(div2);


            // 残り時間表示
            _timeLabel = new global::Android.Widget.TextView(themedContext)
            {
                Text = FormatTime(TimerForegroundService.RemainingSeconds),
                TextSize = 36
            };
            _timeLabel.SetTextColor(global::Android.Graphics.Color.White);
            _timeLabel.Gravity = global::Android.Views.GravityFlags.CenterHorizontal;
            // ボタンを横並びにする行
            var buttonRow = new global::Android.Widget.LinearLayout(themedContext)
            {
                Orientation = global::Android.Widget.Orientation.Horizontal
            };

            var startButton = new global::Android.Widget.Button(themedContext) { Text = "START" };
            startButton.SetTextColor(global::Android.Graphics.Color.White);
            startButton.SetBackgroundColor(global::Android.Graphics.Color.Argb(127, 0, 30, 60));
            startButton.Click += (s, e) => _timerController.Start(TimeSpan.FromSeconds(Settings.DurationSeconds));


            var stopButton = new global::Android.Widget.Button(themedContext) { Text = "STOP" };
            stopButton.SetTextColor(global::Android.Graphics.Color.White);
            stopButton.SetBackgroundColor(global::Android.Graphics.Color.Argb(127, 0, 30, 60));
            stopButton.Click += (s, e) =>
            {
                _timerController.Stop();
                _timeLabel?.Text = FormatTime(0);
            };

            var buttonParams = new global::Android.Widget.LinearLayout.LayoutParams(
                global::Android.Views.ViewGroup.LayoutParams.WrapContent, global::Android.Views.ViewGroup.LayoutParams.WrapContent);
            buttonParams.SetMargins(20, 0, 4, 0);


            buttonRow.AddView(startButton, buttonParams);
            buttonRow.AddView(stopButton, buttonParams);

            container.AddView(_timeLabel);
            container.AddView(buttonRow);
            _overlayView = container;

            var overlayType = Build.VERSION.SdkInt >= BuildVersionCodes.O
                ? global::Android.Views.WindowManagerTypes.ApplicationOverlay
                : global::Android.Views.WindowManagerTypes.Phone;

            var layoutParams = new global::Android.Views.WindowManagerLayoutParams(
                global::Android.Views.WindowManagerLayoutParams.WrapContent,
                global::Android.Views.WindowManagerLayoutParams.WrapContent,
                overlayType,
                global::Android.Views.WindowManagerFlags.NotFocusable,
                global::Android.Graphics.Format.Translucent)
            {
                Gravity = global::Android.Views.GravityFlags.Bottom | global::Android.Views.GravityFlags.Left,
                X = 100,
                Y = 300
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
        if (_stationLabel?.Text == "")
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
            if (lastStationName != "" && lastStationName != stationName)
            {
                Vibrate();
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
}