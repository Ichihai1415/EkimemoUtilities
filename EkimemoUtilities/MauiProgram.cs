using EkimemoUtilities.Services;
using EkimemoUtilities.Utils;
using Microsoft.Extensions.Logging;

namespace EkimemoUtilities
{
    public static class MauiProgram
    {
        internal static WhatPolygonIs.GeoJSON? geojson = null;

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

            using var stream = FileSystem.OpenAppPackageFileAsync("voronoi.geojson").Result;
            using var reader = new StreamReader(stream);
            var contents = reader.ReadToEnd();
            geojson = new WhatPolygonIs.GeoJSON(contents);


#if ANDROID
            builder.Services.AddSingleton<IOverlayService, EkimemoUtilities.Platforms.Android.OverlayService>();
            builder.Services.AddSingleton<ITimerController, EkimemoUtilities.Platforms.Android.TimerController>();
            builder.Services.AddSingleton<ILocationTracker, EkimemoUtilities.Platforms.Android.LocationTracker>();

#endif

            builder.Services.AddTransient<MainPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
