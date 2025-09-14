using Microsoft.Extensions.Logging;
using CoralClientMobileApp.DbContext;
using CoralClientMobileApp.View;
using CoralClientMobileApp.ViewModel;
using CoralClientMobileApp.Model;
using MinecraftRcon;
using DotNet.Meteor.HotReload.Plugin;
using MinecraftQuery;

namespace CoralClientMobileApp;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
#if DEBUG
            .EnableHotReload()
#endif
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
		
		// Register factory for RconPageViewModel since it needs ServerProfile parameter
		builder.Services.AddTransient<Func<ServerProfile, RconPageViewModel>>(serviceProvider =>
			serverProfile => new RconPageViewModel(serverProfile, 
				serviceProvider.GetRequiredService<RconClient>(),
				serviceProvider.GetRequiredService<ILogger<RconPageViewModel>>()));

		// Register services
		builder.Services.AddDbContext<ServerProfileContext>();
		builder.Services.AddTransient<RconClient>();
		builder.Services.AddTransient<MinecraftQueryClient>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
