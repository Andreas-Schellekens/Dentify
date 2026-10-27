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

            // View models are singletons so they stay registered for messages
            builder.Services.AddSingleton<VehicleListViewModel>();
            builder.Services.AddSingleton<VehicleDetailViewModel>();
            builder.Services.AddSingleton<SettingsViewModel>();

            // Pages
            builder.Services.AddTransient<VehicleListPage>();
            builder.Services.AddTransient<VehicleDetailPage>();
            builder.Services.AddTransient<SettingsPage>();

            // Routes for pages that are pushed on top of the root page
            Routing.RegisterRoute(nameof(VehicleDetailPage), typeof(VehicleDetailPage));
            Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            var app = builder.Build();

            // Create the view models that receive messages up front, so no message is sent before they listen
            app.Services.GetRequiredService<VehicleDetailViewModel>();

            return app;
        }
    }
}
