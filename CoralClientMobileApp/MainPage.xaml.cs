using CoralClientMobileApp.DbContext;
using CoralClientMobileApp.Services;
using CoralClientMobileApp.View;
using CoralClientMobileApp.ViewModel;
using Microsoft.Extensions.DependencyInjection;

namespace CoralClientMobileApp;

public partial class MainPage : ContentPage
{
	int count = 0;

	public MainPage()
	{
		InitializeComponent();

		var vm = new MainPageViewModel(Dependencies.ServiceProvider.GetService<ServerProfileContext>(),
                (title, message) => DisplayPromptAsync(title, message),
                (serverProfile) => Navigation.PushModalAsync(new RconPage(serverProfile)));

            LogoImage.Source = ImageSource.FromResource("CoralClientMobileApp.Assets.icon.png", GetType().Assembly);

            BindingContext = vm;
	}
}

