using EkimemoUtilities.Services;

namespace EkimemoUtilities
{
    public partial class MainPage : ContentPage
    {
        int count = 0;

        private readonly IOverlayService _overlayService;

        private readonly ITimerController _timerController;
        public MainPage(IOverlayService overlayService, ITimerController timerController)
        {
            InitializeComponent();
            _overlayService = overlayService;
            _timerController = timerController;
        }


        protected override void OnAppearing()
        {
            base.OnAppearing();

            // 画面表示のたびに、保存済みの設定をUIに反映する
            var seconds = Settings.DurationSeconds;
            DurationStepper.Value = seconds;
            DurationLabel.Text = seconds.ToString();
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
