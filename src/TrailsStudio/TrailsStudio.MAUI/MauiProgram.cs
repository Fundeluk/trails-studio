using TrailsStudio.MAUI.Evergine;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using TrailsStudio.MAUI.ViewModels;

namespace TrailsStudio.MAUI
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseMauiEvergine()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Register services
            builder.Services.AddTransient<RollInParamsViewModel>();
            builder.Services.AddTransient<StudioViewModel>();
            builder.Services.AddTransient<StudioPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}