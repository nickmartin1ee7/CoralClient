using CoralClientMobileApp.Model;
using Microsoft.Extensions.Logging;

namespace CoralClientMobileApp.View;

public partial class ServerProfileEditPage : ContentPage
{
    private TaskCompletionSource<ServerProfile?>? _taskCompletionSource;
    private readonly ServerProfileEditViewModel _viewModel;
    private readonly ILogger<ServerProfileEditPage> _logger;

    public ServerProfileEditPage(ILogger<ServerProfileEditPage> logger)
    {
        InitializeComponent();
        _logger = logger;
        _viewModel = new ServerProfileEditViewModel();
        BindingContext = _viewModel;
    }

    public Task<ServerProfile?> ShowAsync(ServerProfile? existingProfile = null)
    {
        _viewModel.Initialize(existingProfile);
        _taskCompletionSource = new TaskCompletionSource<ServerProfile?>();
        return _taskCompletionSource.Task;
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        try
        {
            var serverProfile = _viewModel.CreateServerProfile();
            
            if (serverProfile == null)
            {
                await DisplayAlert("Validation Error", "Please fill in all required fields.", "OK");
                return;
            }

            _taskCompletionSource?.SetResult(serverProfile);
            await Navigation.PopModalAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        try
        {
            _taskCompletionSource?.SetResult(null);
            await Navigation.PopModalAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while canceling server profile edit");
        }
    }
}