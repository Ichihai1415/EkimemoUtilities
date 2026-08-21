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

        public MainPage(IOverlayService overlayService)
        {
            InitializeComponent();
            _overlayService = overlayService;
        }

        private void OnToggleOverlayClicked(object sender, EventArgs e)
        {
            _overlayService.Toggle();
            ToggleOverlayButton.Text = _overlayService.IsShowing
                ? "ポップアップを非表示"
                : "ポップアップを表示";
        }
    }
}
