using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Provider;
using Android.Views;
using Android.Widget;
using EkimemoUtilities.Services;
using AndroidUri = Android.Net.Uri;
using AView = Android.Views.View;
using AApplication = Android.App.Application;
using AButton = Android.Widget.Button;
using AColor = Android.Graphics.Color;
using ALog = Android.Util.Log;
using Android.Runtime;
using Microsoft.Maui.ApplicationModel;

namespace EkimemoUtilities.Platforms.Android;

public class OverlayService : IOverlayService
{
    private IWindowManager? _windowManager;
    private AView? _overlayView;

    public bool IsShowing { get; private set; }

    public bool HasPermission()
        => Settings.CanDrawOverlays(AApplication.Context);

    public void RequestPermission()
    {
        var intent = new Intent(
            Settings.ActionManageOverlayPermission,
            AndroidUri.Parse($"package:{AApplication.Context.PackageName}"));
        intent.SetFlags(ActivityFlags.NewTask);
        AApplication.Context.StartActivity(intent);
    }

    public void Show()
    {
        ALog.Debug("OverlayService", $"Show called. IsShowing={IsShowing}");
        if (IsShowing) return;

        if (!HasPermission())
        {
            RequestPermission();
            return;
        }

        try
        {
            ALog.Debug("OverlayService", $"Show try (dispatch to main thread)");

            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    var context = AApplication.Context;
                    var themedContext = new ContextThemeWrapper(
                        context, global::Android.Resource.Style.ThemeMaterialLight);

                    // Try themedContext first (Activity/Theme-aware), then application context.
                    IJavaObject? sys = themedContext.GetSystemService(Context.WindowService) as IJavaObject
                        ?? context.GetSystemService(Context.WindowService) as IJavaObject;
                    try
                    {
                        _windowManager = sys != null ? sys.JavaCast<IWindowManager>() : null;
                    }
                    catch (Exception castEx)
                    {
                        ALog.Warn("OverlayService", $"WindowManager cast failed: {castEx}");
                        _windowManager = null;
                    }
                    if (_windowManager is null)
                    {
                        ALog.Warn("OverlayService", "GetSystemService returned null for WindowService");
                    }

                    var button = new AButton(themedContext) { Text = "ああああ×ああああ" };
                    button.SetBackgroundColor(AColor.Red);
                    button.SetTextColor(AColor.White);
                    button.Click += (s, e) => Hide();
                    _overlayView = button;

                    var overlayType = Build.VERSION.SdkInt >= BuildVersionCodes.O
                        ? WindowManagerTypes.ApplicationOverlay
                        : WindowManagerTypes.Phone;

                    var layoutParams = new WindowManagerLayoutParams(
                        300,
                        300,
                        overlayType,
                        WindowManagerFlags.NotFocusable,
                        Format.Translucent)
                    {
                        Gravity = GravityFlags.Center,
                        X = 10,
                        Y = 30
                    };

                    ALog.Debug("OverlayService", $"_windowManager is null: {_windowManager == null}");
                    _windowManager?.AddView(_overlayView, layoutParams);
                    IsShowing = true;
                    ALog.Debug("OverlayService", $"Show added view on main thread");
                }
                catch (Exception ex)
                {
                    ALog.Error("OverlayService", $"表示に失敗 (main thread): {ex}");
                }
            });
        }
        catch (Exception ex)
        {
            ALog.Error("OverlayService", $"表示に失敗: {ex}");
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
        ALog.Debug("OverlayService", $"Toggle called. IsShowing={IsShowing}");
        if (IsShowing) Hide();
        else Show();
    }
}