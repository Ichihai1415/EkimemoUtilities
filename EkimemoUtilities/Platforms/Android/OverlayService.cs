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
    public OverlayService(ITimerController timerController)
    {
        _timerController = timerController;
    }

    private global::Android.Views.IWindowManager? _windowManager;
    private global::Android.Views.View? _overlayView;
    private global::Android.Widget.TextView? _timeLabel;
    private readonly Handler _mainHandler = new(Looper.MainLooper!);

    private readonly ITimerController _timerController;

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
                Text = "--------------------",
                TextSize = 20
            };
            div.SetTextColor(global::Android.Graphics.Color.LightGray);
            div.Gravity = global::Android.Views.GravityFlags.CenterHorizontal;

            container.AddView(div);


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
        }
        catch (Exception ex)
        {
            global::Android.Util.Log.Error("OverlayService", $"表示に失敗: {ex}");
        }
    }

    public void Hide()
    {
        if (!IsShowing || _overlayView is null) return;

        _windowManager?.RemoveView(_overlayView);
        _overlayView = null;
        IsShowing = false;
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
}