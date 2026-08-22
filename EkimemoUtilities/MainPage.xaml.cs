using EkimemoUtilities.Services;

namespace EkimemoUtilities
{
    public partial class MainPage : ContentPage
    {
        int count = 0;

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




        private async void OnStartLocationClicked(object sender, EventArgs e)
        {
            var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                await DisplayAlert("権限が必要です", "位置情報の許可が必要です。", "OK");
                return;
            }

            _locationTracker.Start();
        }

        private void OnStopLocationClicked(object sender, EventArgs e)
        {
            _locationTracker.Stop();
        }


        protected override void OnAppearing()
        {
            base.OnAppearing();

            // 画面表示のたびに、保存済みの設定をUIに反映する
            var secondsDS = Settings.DurationSeconds;
            DurationStepper.Value = secondsDS;
            DurationLabel.Text = secondsDS.ToString();

            var secondsI = Settings.IntervalSeconds;
            IntervalStepper.Value = secondsI;
            IntervalLabel.Text = secondsI.ToString();

            //自動表示
            _overlayService.Show();
            ToggleOverlayButton.Text = _overlayService.IsShowing
                 ? "Hide Overlay"
                : "Show Overlay";
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
            ToggleOverlayButton.Text = _overlayService.IsShowing
                ? "Hide Overlay"
                : "Show Overlay";
        }

        private async void OnStartClicked(object sender, EventArgs e)
        {
            var locationStatus = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (locationStatus != PermissionStatus.Granted)
            {
                await DisplayAlertAsync("権限が必要です", "位置情報の許可が必要です。", "OK");
                return;
            }

#if ANDROID
            if (OperatingSystem.IsAndroidVersionAtLeast(33))
            {
                await Permissions.RequestAsync<Platforms.Android.PostNotificationsPermission>();
                // 拒否されても致命的ではないので、ここではエラー扱いにしない
            }
#endif

            _timerController.Start(TimeSpan.FromSeconds(Settings.DurationSeconds));
        }
    }
}
