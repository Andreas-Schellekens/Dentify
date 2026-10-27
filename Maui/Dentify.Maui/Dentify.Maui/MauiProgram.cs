using Dentify.Maui.Services;
using Dentify.Maui.ViewModels;
using Dentify.Maui.Views;
using Microsoft.Extensions.Logging;

namespace Dentify.Maui
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

            // Services
            builder.Services.AddSingleton<ISettingsService, SettingsService>();
            builder.Services.AddSingleton<INavigationService, NavigationService>();
            builder.Services.AddHttpClient<IApiService, ApiService>(client =>
            {
                // Uploading photos runs the Azure models per photo, which can take a while
                client.Timeout = TimeSpan.FromMinutes(3);
                // Skips the Ngrok free tier warning page
                client.DefaultRequestHeaders.Add("ngrok-skip-browser-warning", "true");
            });
            builder.Services.AddSingleton<IPhotoLoaderService, PhotoLoaderService>();
            builder.Services.AddSingleton<IPhotoService, PhotoService>();

            // View models that receive messages are singletons so they stay registered
            builder.Services.AddSingleton<VehicleListViewModel>();
            builder.Services.AddSingleton<VehicleDetailViewModel>();
            builder.Services.AddSingleton<NewInspectionViewModel>();
            builder.Services.AddSingleton<InspectionResultViewModel>();
            builder.Services.AddSingleton<SettingsViewModel>();
            // A new scan always starts clean
            builder.Services.AddTransient<PlateScanViewModel>();

            // Pages
            builder.Services.AddTransient<VehicleListPage>();
            builder.Services.AddTransient<VehicleDetailPage>();
            builder.Services.AddTransient<PlateScanPage>();
            builder.Services.AddTransient<NewInspectionPage>();
            builder.Services.AddTransient<InspectionResultPage>();
            builder.Services.AddTransient<SettingsPage>();

            // Routes for pages that are pushed on top of the root page
            Routing.RegisterRoute(nameof(VehicleDetailPage), typeof(VehicleDetailPage));
            Routing.RegisterRoute(nameof(PlateScanPage), typeof(PlateScanPage));
            Routing.RegisterRoute(nameof(NewInspectionPage), typeof(NewInspectionPage));
            Routing.RegisterRoute(nameof(InspectionResultPage), typeof(InspectionResultPage));
            Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            var app = builder.Build();

            // Create the view models that receive messages up front, so no message is sent before they listen
            app.Services.GetRequiredService<VehicleDetailViewModel>();
            app.Services.GetRequiredService<NewInspectionViewModel>();
            app.Services.GetRequiredService<InspectionResultViewModel>();

            return app;
        }
    }
}
