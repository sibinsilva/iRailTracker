using iRailTracker.Service;
using iRailTracker.View;
using iRailTracker.ViewModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Reflection;
using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models.AndroidOption;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace iRailTracker
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseSkiaSharp()
                .UseLocalNotification(config =>
                {
                    config.AddAndroid(android =>
                    {
                        android.AddChannel(new AndroidNotificationChannelRequest
                        {
                            Id = "journey_tracking",
                            Name = "Journey Tracking",
                            Description = "Ongoing status while a journey is being tracked",
                            Importance = AndroidImportance.Low,
                            EnableVibration = false,
                            EnableSound = false
                        });
                    });
                })
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif
            builder.Services.AddSingleton(typeof(DataService<>));
            builder.Services.AddSingleton<ConfigLoader>();
            builder.Services.AddTransient<AppHomeViewModel>();
            builder.Services.AddSingleton<AppSettingsViewModel>();
            builder.Services.AddTransient<AppSettings>();
            builder.Services.AddSingleton<AutoRefreshService>();
            var a = Assembly.GetExecutingAssembly();
            using var stream = a.GetManifestResourceStream("iRailTracker.appsettings.json");
            if (stream == null)
                throw new InvalidOperationException("Config stream is null.");

            var config = new ConfigurationBuilder()
                .AddJsonStream(stream)
                .Build();
            builder.Configuration.AddConfiguration(config);

            var app = builder.Build();
            Current = app.Services;
            return app;
        }
        public static IServiceProvider? Current { get; private set; }
    }
}
