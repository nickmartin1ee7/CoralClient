using CoralClientMobileApp.View;
using CoralClientMobileApp.ViewModel;
using CoralClientMobileApp.Model;
using Microsoft.Extensions.DependencyInjection;

namespace CoralClientMobileApp;

public partial class MainPage : ContentPage, IDisposable
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
			(existingProfile) => ShowServerProfileEditModal(existingProfile),
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
		
		// Start polling when the main page appears
		_viewModel.StartPolling();
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		
		// Stop polling when navigating away from the main page
		_viewModel.StopPolling();
	}

	private async Task<ServerProfile?> ShowServerProfileEditModal(ServerProfile? existingProfile)
	{
		var editPage = _serviceProvider.GetRequiredService<ServerProfileEditPage>();
		await Navigation.PushModalAsync(editPage);
		return await editPage.ShowAsync(existingProfile);
	}

	private async Task NavigateToRconPage(ServerProfile serverProfile)
	{
		var rconPage = _serviceProvider.GetRequiredService<RconPage>();
		rconPage.Initialize(serverProfile);
		await Navigation.PushModalAsync(rconPage);
	}

	public void Dispose()
	{
		_viewModel?.Dispose();
	}
}

