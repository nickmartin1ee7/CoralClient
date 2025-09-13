using Microsoft.Extensions.Logging;
using CoralClientMobileApp.DbContext;
using CoralClientMobileApp.View;
using CoralClientMobileApp.ViewModel;
using MinecraftRcon;

namespace CoralClientMobileApp;

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

		// Register Views
		builder.Services.AddSingleton<AppShell>();
		builder.Services.AddSingleton<MainPage>();
		builder.Services.AddTransient<RconPage>();

		// Register ViewModels
		builder.Services.AddSingleton<MainPageViewModel>();
		// Note: RconPageViewModel needs special handling since it requires ServerProfile parameter

		// Register services
		builder.Services.AddDbContext<ServerProfileContext>();
		builder.Services.AddTransient<RconClient>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
