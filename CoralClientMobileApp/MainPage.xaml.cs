using CoralClientMobileApp.View;
using CoralClientMobileApp.ViewModel;
using CoralClientMobileApp.Model;
using Microsoft.Extensions.DependencyInjection;

namespace CoralClientMobileApp;

public partial class MainPage : ContentPage
{
	private readonly MainPageViewModel _viewModel;
	private readonly IServiceProvider _serviceProvider;

	public MainPage(MainPageViewModel viewModel, IServiceProvider serviceProvider)
	{
		InitializeComponent();
		_viewModel = viewModel;
		_serviceProvider = serviceProvider;

		// Set dependencies that need the Page context
		_viewModel.Initialize(
			(title, message) => DisplayPromptAsync(title, message),
			(serverProfile) => NavigateToRconPage(serverProfile)
		);

		LogoImage.Source = "icon.png"; // Use the icon.png from Resources/Images

		BindingContext = _viewModel;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		
		// Initialize the ViewModel and load data when the page appears
		await _viewModel.InitializeAsync();
	}

	private async Task NavigateToRconPage(ServerProfile serverProfile)
	{
		var rconPage = _serviceProvider.GetRequiredService<RconPage>();
		rconPage.Initialize(serverProfile);
		await Navigation.PushModalAsync(rconPage);
	}
}

