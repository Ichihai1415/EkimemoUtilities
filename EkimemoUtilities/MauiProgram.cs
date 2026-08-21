using Microsoft.Extensions.Logging;
using EkimemoUtilities.Services;

namespace EkimemoUtilities
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if ANDROID
            builder.Services.AddSingleton<IOverlayService, EkimemoUtilities.Platforms.Android.OverlayService>();
            builder.Services.AddSingleton<ITimerController, EkimemoUtilities.Platforms.Android.TimerController>();

#endif

            builder.Services.AddTransient<MainPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
