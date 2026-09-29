using EkimemoUtilities.Services;

namespace EkimemoUtilities
{
    public partial class MainPage : ContentPage
    {
        private readonly IOverlayService _overlayService;

        private readonly ITimerController _timerController;
        private readonly ILocationTracker _locationTracker;

        const string Version = "v1.0.4";
        public MainPage(IOverlayService overlayService, ITimerController timerController, ILocationTracker locationTracker)
        {
            InitializeComponent();
            _overlayService = overlayService;
            _timerController = timerController;
            _locationTracker = locationTracker;
            this.Loaded += async (_, __) => await InitialProcess();
        }

        private async Task InitialProcess()
        {
            try
            {
                var client = new HttpClient();
                var newVersion = await client.GetStringAsync("https://raw.githubusercontent.com/Ichihai1415/EkimemoUtilities/refs/heads/master/EkimemoUtilities/version.txt");
                if (newVersion != Version)
                {
                    var result = await DisplayActionSheetAsync($"Update released!\n {Version} -> {newVersion}", "cancel", null, "tap to open release page");

                    if (result == "open release page")
                    {
                        await Launcher.OpenAsync("https://github.com/Ichihai1415/EkimemoUtilities/releases/latest");
                    }

                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", "[MainPage.InitialProcess] 更新確認に失敗しました。\n" + ex.ToString(), "OK");
            }
        }


        private async void OnToggleLocationClicked(object sender, EventArgs e)
        {
            try
            {
                if (_locationTracker.IsRunning)
                {
                    _locationTracker.Stop();
                }
                else
                {
                    var status_loc = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                    if (status_loc != PermissionStatus.Granted)
                        status_loc = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                    if (status_loc != PermissionStatus.Granted)
                    {
                        await DisplayAlertAsync("権限が必要です", "位置情報の許可が必要です。", "OK");
                        return;
                    }

                    _locationTracker.Start();

                    if (Settings.NotificationEnabled)//if (OperatingSystem.IsAndroidVersionAtLeast(33))
                    {
                        var status_not = await Permissions.CheckStatusAsync<Platforms.Android.PostNotificationsPermission>();
                        if (status_not != PermissionStatus.Granted)
                            _ = await Permissions.RequestAsync<Platforms.Android.PostNotificationsPermission>();
                    }
                }
                ToggleLocationButton.Text = !_locationTracker.IsRunning ? "Stop Getting Location" : "Start Getting Location";
                ToggleLocationButton.BackgroundColor = !_locationTracker.IsRunning ? Color.FromArgb("#2B0B98") : Color.FromArgb("#512BD4");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", "[MainPage.OnToggleLocationClicked] 処理に失敗しました。\n" + ex.ToString(), "OK");
            }
        }

        protected override void OnAppearing()
        {
            try
            {
                base.OnAppearing();

                var secondsDS = Settings.DurationSeconds;
                DurationStepper.Value = secondsDS;
                DurationLabel.Text = secondsDS.ToString();

                var secondsI = Settings.IntervalSeconds;
                IntervalStepper.Value = secondsI;
                IntervalLabel.Text = secondsI.ToString();

                var gpsWait100ms = Settings.GPSWaitSeconds;
                GPSWaitStepper.Value = gpsWait100ms;
                GPSWaitLabel.Text = gpsWait100ms.ToString("0.0");

                var snsDistance = Settings.NearSt_Distance;
                SNSDistanceStepper.Value = snsDistance;
                SNSDistanceLabel.Text = snsDistance.ToString();

                var snsCount = Settings.NearSt_MaxCount;
                SNSCountStepper.Value = snsCount;
                SNSCountLabel.Text = snsCount.ToString();

                _overlayService.Show();
                ToggleOverlayButton.Text = _overlayService.IsShowing ? "Hide Overlay" : "Show Overlay";
                ToggleOverlayButton.BackgroundColor = _overlayService.IsShowing ? Color.FromArgb("#2B0B98") : Color.FromArgb("#512BD4");
                ToggleLocationButton.Text = _locationTracker.IsRunning ? "Stop Getting Location" : "Start Getting Location";
                ToggleLocationButton.BackgroundColor = _locationTracker.IsRunning ? Color.FromArgb("#2B0B98") : Color.FromArgb("#512BD4");

                VibrationCheckBox.IsChecked = Settings.VibrationEnabled;
                NotificationCheckBox.IsChecked = Settings.NotificationEnabled;
                HideLocationCheckBox.IsChecked = Settings.HideLocationEnabled;
                HideTimerCheckBox.IsChecked = Settings.HideTimerEnabled;
                ResetTimerOnlyGPSCheckBox.IsChecked = Settings.ResetTimer_OnlyGPS;

            }
            catch (Exception ex)
            {
                DisplayAlertAsync("Error", "[MainPage.OnAppearing] メイン画面表示処理に失敗しました。\n" + ex.ToString(), "OK");
            }
        }

        private void OnDurationChanged(object sender, ValueChangedEventArgs e)
        {
            var seconds = (int)e.NewValue;
            DurationLabel.Text = seconds.ToString();
            Settings.DurationSeconds = seconds;
        }

        private void OnIntervalChanged(object sender, ValueChangedEventArgs e)
        {
            var seconds = (int)e.NewValue;
            IntervalLabel.Text = seconds.ToString();
            Settings.IntervalSeconds = seconds;
        }

        private void OnGPSWaitChanged(object sender, ValueChangedEventArgs e)
        {
            var value = (double)e.NewValue;
            GPSWaitLabel.Text = value.ToString("0.0");
            Settings.GPSWaitSeconds = value;
        }

        private void OnToggleOverlayClicked(object sender, EventArgs e)
        {
            _overlayService.Toggle();
            ToggleOverlayButton.Text = _overlayService.IsShowing ? "Hide Overlay" : "Show Overlay";
            ToggleOverlayButton.BackgroundColor = _overlayService.IsShowing ? Color.FromArgb("#2B0B98") : Color.FromArgb("#512BD4");
        }

        private async void OnStartClicked(object sender, EventArgs e)
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(33))
                await Permissions.RequestAsync<Platforms.Android.PostNotificationsPermission>();
            _timerController.Start(TimeSpan.FromSeconds(Settings.DurationSeconds));
        }

        private void OnVibrationCheckBoxChanged(object sender, CheckedChangedEventArgs e)
        {
            Settings.VibrationEnabled = e.Value;
        }

        private async void OnNotificationCheckBoxChanged(object sender, CheckedChangedEventArgs e)
        {
            Settings.NotificationEnabled = e.Value;
            if (e.Value)//if (OperatingSystem.IsAndroidVersionAtLeast(33))
            {
                var status_not = await Permissions.CheckStatusAsync<Platforms.Android.PostNotificationsPermission>();
                if (status_not != PermissionStatus.Granted)
                    _ = await Permissions.RequestAsync<Platforms.Android.PostNotificationsPermission>();
            }
        }

        private void OnHideLocationCheckBoxChanged(object sender, CheckedChangedEventArgs e)
        {
            Settings.HideLocationEnabled = e.Value;
            if (_overlayService.IsShowing)
                _overlayService.Show();
        }

        private void OnHideTimerCheckBoxChanged(object sender, CheckedChangedEventArgs e)
        {
            Settings.HideTimerEnabled = e.Value;
            if (_overlayService.IsShowing)
                _overlayService.Show();
        }

        private void OnVibrationLabelTapped(object sender, TappedEventArgs e)
        {
            VibrationCheckBox.IsChecked = !VibrationCheckBox.IsChecked;
        }

        private void OnNotificationLabelTapped(object sender, TappedEventArgs e)
        {
            NotificationCheckBox.IsChecked = !NotificationCheckBox.IsChecked;
        }

        private void OnHideLocationLabelTapped(object sender, TappedEventArgs e)
        {
            HideLocationCheckBox.IsChecked = !HideLocationCheckBox.IsChecked;
        }

        private void OnHideTimerLabelTapped(object sender, TappedEventArgs e)
        {
            HideTimerCheckBox.IsChecked = !HideTimerCheckBox.IsChecked;
        }

        private void SNSDistanceChanged(object sender, ValueChangedEventArgs e)
        {
            var value = (int)e.NewValue;
            SNSDistanceLabel.Text = value.ToString();
            Settings.NearSt_Distance = value;
        }

        private void SNSCountChanged(object sender, ValueChangedEventArgs e)
        {
            var value = (int)e.NewValue;
            SNSCountLabel.Text = value.ToString();
            Settings.NearSt_MaxCount = value;
        }


        private void OnResetTimerOnlyGPSCheckBoxChanged(object sender, CheckedChangedEventArgs e)
        {
            Settings.ResetTimer_OnlyGPS = e.Value;
        }

        private void OnResetTimerOnlyGPSLabelTapped(object sender, TappedEventArgs e)
        {
            ResetTimerOnlyGPSCheckBox.IsChecked = !ResetTimerOnlyGPSCheckBox.IsChecked;
        }
    }
}
