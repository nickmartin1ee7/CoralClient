using CoralClientMobileApp.View;
using CoralClientMobileApp.ViewModel;
using CoralClientMobileApp.Model;

namespace CoralClientMobileApp;

public partial class MainPage : ContentPage
{
	private readonly MainPageViewModel _viewModel;

	public MainPage(MainPageViewModel viewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;

		// Set dependencies that need the Page context
		_viewModel.SetDependencies(
			(title, message) => DisplayPromptAsync(title, message),
			(serverProfile) => NavigateToRconPage(serverProfile)
		);

		LogoImage.Source = ImageSource.FromResource("CoralClientMobileApp.Assets.icon.png", GetType().Assembly);

		BindingContext = _viewModel;
	}

	private async Task NavigateToRconPage(ServerProfile serverProfile)
	{
		var rconPage = new RconPage(serverProfile);
		await Navigation.PushModalAsync(rconPage);
	}
}

