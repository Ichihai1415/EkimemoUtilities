using EkimemoUtilities.Services;

namespace EkimemoUtilities
{
    public partial class MainPage : ContentPage
    {
        int count = 0;

        public MainPage()
        {
            InitializeComponent();
        }

        private void OnCounterClicked(object? sender, EventArgs e)
        {
            count++;

            if (count == 1)
                CounterBtn.Text = $"Clicked {count} time";
            else
                CounterBtn.Text = $"Clicked {count} times";

            SemanticScreenReader.Announce(CounterBtn.Text);
        }

        private readonly IOverlayService _overlayService;
        private readonly ITimerController _timerController;

        public MainPage(IOverlayService overlayService, ITimerController timerController)
        {
            InitializeComponent();
            _overlayService = overlayService;
            _timerController = timerController;
        }

        private void OnToggleOverlayClicked(object sender, EventArgs e)
        {
            _overlayService.Toggle();
            ToggleOverlayButton.Text = _overlayService.IsShowing
                ? "ポップアップを非表示"
                : "ポップアップを表示";
        }

        private async void OnStartClicked(object sender, EventArgs e)
        {
            var locationStatus = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (locationStatus != PermissionStatus.Granted)
            {
                await DisplayAlert("権限が必要です", "タイマー機能を使うには位置情報の許可が必要です。", "OK");
                return;
            }

#if ANDROID
    if (OperatingSystem.IsAndroidVersionAtLeast(33))
    {
        await Permissions.RequestAsync<Platforms.Android.PostNotificationsPermission>();
        // 拒否されても致命的ではないので、ここではエラー扱いにしない
    }
#endif

            _timerController.Start(TimeSpan.FromMinutes(5));
        }
    }
}
