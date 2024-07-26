using iRailTracker.Service;
using iRailTracker.View;
using iRailTracker.ViewModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace iRailTracker
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

#if DEBUG
    		builder.Logging.AddDebug();
#endif
            builder.Services.AddSingleton(typeof(DataService<>));
            builder.Services.AddSingleton<ConfigLoader>();
            builder.Services.AddTransient<AppHomeViewModel>();
            var a = Assembly.GetExecutingAssembly();
            using var stream = a.GetManifestResourceStream("iRailTracker.appsettings.json");
            var config = new ConfigurationBuilder()
            .AddJsonStream(stream)
            .Build();
            builder.Configuration.AddConfiguration(config);

            var app = builder.Build();
            Current = app.Services;
            return app;
        }
        public static IServiceProvider Current { get; private set; }
    }
}
