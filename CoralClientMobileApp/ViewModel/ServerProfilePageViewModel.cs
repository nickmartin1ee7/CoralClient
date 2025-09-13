using System;
using System.Threading.Tasks;
using CoralClientMobileApp.Model;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace CoralClientMobileApp.ViewModel
{
    public partial class ServerProfilePageViewModel : BaseObservableViewModel
    {
        private readonly ILogger<ServerProfilePageViewModel> _logger;
        private Func<Task>? _closePageFuncAsync;
        private Func<ServerProfile, Task>? _saveServerProfileFuncAsync;
        private TaskCompletionSource<ServerProfile?>? _resultTaskSource;
        private ServerProfile? _existingProfile;

        [ObservableProperty]
        private string serverUri = string.Empty;

        [ObservableProperty]
        private string minecraftPort = "25565";

        [ObservableProperty]
        private string rconPort = "25575";

        [ObservableProperty]
        private string rconPassword = string.Empty;

        [ObservableProperty]
        private string serverName = string.Empty;

        [ObservableProperty]
        private string serverDescription = string.Empty;

        [ObservableProperty]
        private bool isTestingConnection = false;

        public ServerProfilePageViewModel(ILogger<ServerProfilePageViewModel> logger)
        {
            _logger = logger;
        }

        public void Initialize(
            Func<Task> closePageFuncAsync,
            Func<ServerProfile, Task> saveServerProfileFuncAsync,
            ServerProfile? existingProfile = null)
        {
            _closePageFuncAsync = closePageFuncAsync;
            _saveServerProfileFuncAsync = saveServerProfileFuncAsync;

            if (existingProfile != null)
            {
                LoadExistingProfile(existingProfile);
            }
        }

        public async Task InitializeAsync(ServerProfile? existingProfile = null)
        {
            _existingProfile = existingProfile;
            _resultTaskSource = new TaskCompletionSource<ServerProfile?>();

            if (existingProfile != null)
            {
                LoadExistingProfile(existingProfile);
            }
        }

        public async Task<ServerProfile?> GetResultAsync()
        {
            if (_resultTaskSource == null)
                return null;

            return await _resultTaskSource.Task;
        }

        private void LoadExistingProfile(ServerProfile profile)
        {
            ServerUri = profile.Uri;
            MinecraftPort = profile.MinecraftPort.ToString();
            RconPort = profile.RconPort.ToString();
            RconPassword = profile.Password;
            // ServerName and ServerDescription would need to be added to the ServerProfile model
        }

        [RelayCommand]
        private async Task TestConnection()
        {
            if (string.IsNullOrWhiteSpace(ServerUri) || string.IsNullOrWhiteSpace(RconPassword))
            {
                await Application.Current.MainPage.DisplayAlert("Validation Error", 
                    "Please enter both server URI and RCON password before testing.", "OK");
                return;
            }

            IsTestingConnection = true;

            try
            {
                _logger.LogInformation("Testing connection to server: {ServerUri}", ServerUri);

                // Here you could implement actual connection testing
                // For now, we'll simulate it
                await Task.Delay(2000);

                await Application.Current.MainPage.DisplayAlert("Connection Test", 
                    "Connection test completed! (This is a simulated test)", "OK");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to test connection to server: {ServerUri}", ServerUri);
                await Application.Current.MainPage.DisplayAlert("Connection Error", 
                    "Failed to connect to the server. Please check your settings.", "OK");
            }
            finally
            {
                IsTestingConnection = false;
            }
        }

        [RelayCommand]
        private async Task Save()
        {
            if (!ValidateForm())
                return;

            try
            {
                var serverProfile = new ServerProfile
                {
                    Uri = ServerUri.ToLower().Trim(),
                    MinecraftPort = ushort.Parse(MinecraftPort),
                    RconPort = ushort.Parse(RconPort),
                    Password = RconPassword.Trim()
                };

                // If editing existing profile, preserve the ID
                if (_existingProfile != null)
                {
                    serverProfile.Id = _existingProfile.Id;
                }

                // Set the result for the TaskCompletionSource
                _resultTaskSource?.SetResult(serverProfile);

                // Legacy callback support
                if (_saveServerProfileFuncAsync != null)
                {
                    await _saveServerProfileFuncAsync(serverProfile);
                }

                if (_closePageFuncAsync != null)
                {
                    await _closePageFuncAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save server profile");
                await Application.Current.MainPage.DisplayAlert("Save Error", 
                    "Failed to save the server profile. Please try again.", "OK");
            }
        }

        [RelayCommand]
        private async Task Cancel()
        {
            // Set null result indicating cancellation
            _resultTaskSource?.SetResult(null);

            if (_closePageFuncAsync != null)
            {
                await _closePageFuncAsync();
            }
        }

        private bool ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(ServerUri))
            {
                Application.Current.MainPage.DisplayAlert("Validation Error", 
                    "Please enter a server URI or IP address.", "OK");
                return false;
            }

            if (!ushort.TryParse(MinecraftPort, out _))
            {
                Application.Current.MainPage.DisplayAlert("Validation Error", 
                    "Please enter a valid Minecraft port number (1-65535).", "OK");
                return false;
            }

            if (!ushort.TryParse(RconPort, out _))
            {
                Application.Current.MainPage.DisplayAlert("Validation Error", 
                    "Please enter a valid RCON port number (1-65535).", "OK");
                return false;
            }

            if (string.IsNullOrWhiteSpace(RconPassword))
            {
                Application.Current.MainPage.DisplayAlert("Validation Error", 
                    "Please enter the RCON password.", "OK");
                return false;
            }

            return true;
        }
    }
}