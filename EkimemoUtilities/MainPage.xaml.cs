using EkimemoUtilities.Services;

namespace EkimemoUtilities
{
    public partial class MainPage : ContentPage
    {
        private readonly IOverlayService _overlayService;

        private readonly ITimerController _timerController;
        private readonly ILocationTracker _locationTracker;
        public MainPage(IOverlayService overlayService, ITimerController timerController, ILocationTracker locationTracker)
        {
            InitializeComponent();
            _overlayService = overlayService;
            _timerController = timerController;
            _locationTracker = locationTracker;
        }


        private async void OnToggleLocationClicked(object sender, EventArgs e)
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

        protected override void OnAppearing()
        {
            base.OnAppearing();

            var secondsDS = Settings.DurationSeconds;
            DurationStepper.Value = secondsDS;
            DurationLabel.Text = secondsDS.ToString();

            var secondsI = Settings.IntervalSeconds;
            IntervalStepper.Value = secondsI;
            IntervalLabel.Text = secondsI.ToString();

            _overlayService.Show();
            ToggleOverlayButton.Text = _overlayService.IsShowing ? "Hide Overlay" : "Show Overlay";
            ToggleOverlayButton.BackgroundColor = _overlayService.IsShowing ? Color.FromArgb("#2B0B98") : Color.FromArgb("#512BD4");
            ToggleLocationButton.Text = _locationTracker.IsRunning ? "Stop Getting Location" : "Start Getting Location";
            ToggleLocationButton.BackgroundColor = _locationTracker.IsRunning ? Color.FromArgb("#2B0B98") : Color.FromArgb("#512BD4");

            VibrationCheckBox.IsChecked = Settings.VibrationEnabled;
            NotificationCheckBox.IsChecked = Settings.NotificationEnabled;

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
        }

        private void OnHideTimerCheckBoxChanged(object sender, CheckedChangedEventArgs e)
        {
            Settings.HideTimerEnabled = e.Value;
        }
    }
}
