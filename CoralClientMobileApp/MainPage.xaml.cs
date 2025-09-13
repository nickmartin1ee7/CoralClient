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
			(serverProfile) => NavigateToRconPage(serverProfile),
			(serverProfile) => NavigateToServerProfilePage(serverProfile)
		);

		// Bind the carousel to the indicator for visual paging
		ServerProfilesIndicator.SetBinding(IndicatorView.ItemsSourceProperty, nameof(MainPageViewModel.ServerProfiles));

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

	private async Task<ServerProfile?> NavigateToServerProfilePage(ServerProfile? serverProfile)
	{
		var serverProfilePage = _serviceProvider.GetRequiredService<ServerProfilePage>();
		var viewModel = _serviceProvider.GetRequiredService<ServerProfilePageViewModel>();
		
		// Initialize the page with the server profile (null for new, existing for edit)
		await viewModel.InitializeAsync(serverProfile);
		serverProfilePage.BindingContext = viewModel;
		
		await Navigation.PushModalAsync(serverProfilePage);
		
		// Wait for the page to be dismissed and return the result
		var result = await viewModel.GetResultAsync();
		
		// Close the modal page
		await Navigation.PopModalAsync();
		
		return result;
	}
}

