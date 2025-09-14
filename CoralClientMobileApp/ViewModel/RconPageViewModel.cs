using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CoralClientMobileApp.Helpers;
using CoralClientMobileApp.Model;
using CoralClientMobileApp.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MinecraftRcon;
using Microsoft.Extensions.Logging;

namespace CoralClientMobileApp.ViewModel
{
    public partial class RconPageViewModel : BaseObservableViewModel, IDisposable
    {

        public enum State
        {
            DISCONNECTED,
            CONNECTING,
            CONNECTED
        }

        private readonly ServerProfile _serverProfile;
        private readonly RconClient _rcon;
        private readonly ILogger<RconPageViewModel> _logger;
        private readonly MinecraftQueryService _queryService;
        private readonly StringBuilder _commandLogBuffer = new StringBuilder();
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private State _currentState;
        private Timer? _queryTimer;

        [ObservableProperty]
        private string _serverNameText = "Server URI";

        [ObservableProperty]
        private string _connectionStatusText = State.DISCONNECTED.ToString();

        [ObservableProperty]
        private string _onlinePlayerText = "Players: ?/?";

        [ObservableProperty]
        private string _toggleConnectionButtonText = "Connect";

        [ObservableProperty]
        private string _commandLogText = string.Empty;

        [ObservableProperty]
        private string _commandEntryText = string.Empty;

        [ObservableProperty]
        private bool _isSendCommandEnabled;

        public event EventHandler? StateChange;

        public State CurrentState
        {
            get => _currentState;
            set
            {
                _currentState = value;
                StateChange?.Invoke(this, EventArgs.Empty);
            }
        }

        public RconPageViewModel(ServerProfile serverProfile, RconClient rcon, ILogger<RconPageViewModel> logger, MinecraftQueryService queryService)
        {
            _serverProfile = serverProfile ?? throw new ArgumentNullException(nameof(serverProfile));
            _rcon = rcon ?? throw new ArgumentNullException(nameof(rcon));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
            ServerNameText = serverProfile.ServerUriText;

            _logger.LogInformation("Initializing RconPageViewModel for server: {ServerUri}", serverProfile.ServerUriText);

            _rcon.Disconnected += (o, e) => CurrentState = State.DISCONNECTED;
            _rcon.Connected += (o, e) => CurrentState = State.CONNECTED;
            _rcon.MessageReceived += (o, e) =>
                WriteToCommandLog("Server", e.Body);
            _rcon.AuthenticationCompleted += OnAuthenticationCompleted;

            StateChange += UiStateChangeLogic;
            StateChange += ConnectedLogic;
            
            // Start polling for server status
            StartStatusPolling();
        }

        [RelayCommand]
        private async Task SendCommand()
        {
            if (CurrentState != State.CONNECTED) 
            {
                _logger.LogWarning("Cannot send command - not connected to server");
                return;
            }

            if (!_rcon.IsAuthenticated)
            {
                _logger.LogWarning("Cannot send command - not authenticated");
                WriteToCommandLog("Error", "Not authenticated - cannot send command");
                return;
            }
            
            if (string.IsNullOrWhiteSpace(CommandEntryText)) 
            {
                _logger.LogWarning("Cannot send empty command");
                return;
            }

            _logger.LogInformation("Sending command: {Command}", CommandEntryText);
            WriteToCommandLog("Client", CommandEntryText);

            try
            {
                await _rcon.SendCommandAsync(CommandEntryText);
                CommandEntryText = string.Empty;
                _logger.LogInformation("Command sent successfully");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to send command: {Command}", CommandEntryText);
                WriteToCommandLog("Error", $"Failed to send command! {e.Message}");
            }
        }

        [RelayCommand]
        private async Task ToggleConnection()
        {
            _logger.LogInformation("Toggling connection - current state: {CurrentState}", CurrentState);
            
            switch (CurrentState)
            {
                case State.CONNECTED:
                    await DisconnectAsync();
                    break;
                case State.DISCONNECTED:
                    await ConnectAsync();
                    break;
            }
        }

        public void Dispose()
        {
            _queryTimer?.Dispose();
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _rcon?.Dispose();
        }

        private async void ConnectedLogic(object? sender, EventArgs args)
        {
            if (CurrentState != State.CONNECTED) return;

            try
            {
                WriteToCommandLog("Info", "Attempting to authenticate...");
                await _rcon.AuthenticateAsync(_serverProfile.Password);
                // Authentication result will be handled by OnAuthenticationCompleted
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to send authentication request");
                WriteToCommandLog("Error", $"Failed to send authentication request! {e.Message}");
            }
        }

        private void OnAuthenticationCompleted(object? sender, bool success)
        {
            if (success)
            {
                _logger.LogInformation("Authentication successful");
                WriteToCommandLog("Info", "Authentication successful");
                // Enable command sending now that we're authenticated
                if (CurrentState == State.CONNECTED)
                {
                    IsSendCommandEnabled = true;
                }
            }
            else
            {
                _logger.LogWarning("Authentication failed - invalid password");
                WriteToCommandLog("Error", "Authentication failed - invalid password");
                // Disconnect on authentication failure
                _ = Task.Run(async () => await DisconnectAsync());
            }
        }

        private void UiStateChangeLogic(object? sender, EventArgs e)
        {
            ConnectionStatusText = CurrentState.ToString();

            switch (CurrentState)
            {
                case State.DISCONNECTED:
                {
                    ServerNameText = _serverProfile.ServerUriText;
                    WriteToCommandLog("Info", "Disconnected");
                    IsSendCommandEnabled = false;
                    ToggleConnectionButtonText = "Connect";
                }
                break;
                case State.CONNECTED:
                {
                    WriteToCommandLog("Info", "Connection established");
                    // Only enable command sending if authenticated
                    IsSendCommandEnabled = _rcon.IsAuthenticated;
                    ToggleConnectionButtonText = "Disconnect";
                }
                break;
                case State.CONNECTING:
                {
                    IsSendCommandEnabled = false;
                    ToggleConnectionButtonText = "Connecting...";
                }
                break;
            }
        }

        private void WriteToCommandLog(string prefix, string text, bool newLine = true)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            var formattedText = $"[{DateTime.Now}] {prefix}: {text.RemoveColorCodes()}";

            if (newLine)
            {
                _commandLogBuffer.AppendLine(formattedText);
            }
            else
            {
                _commandLogBuffer.Append(formattedText);
            }

            CommandLogText = _commandLogBuffer.ToString();
        }

        private async Task ConnectAsync()
        {
            CurrentState = State.CONNECTING;
            _logger.LogInformation("Attempting to connect to {ServerUri}:{RconPort}", _serverProfile.Uri, _serverProfile.RconPort);

            try
            {
                var host = await Dns.GetHostEntryAsync(_serverProfile.Uri);
                var targetAddress = host.AddressList.First();

                WriteToCommandLog("Info", $"Establishing connection to {targetAddress}:{_serverProfile.MinecraftPort}");
                _logger.LogInformation("Resolved {ServerUri} to {TargetAddress}", _serverProfile.Uri, targetAddress);

                await _rcon.ConnectAsync(targetAddress.ToString(), _serverProfile.RconPort);
                _logger.LogInformation("Successfully connected to RCON server");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to connect to RCON server {ServerUri}:{RconPort}", _serverProfile.Uri, _serverProfile.RconPort);
                await DisconnectAsync();
                WriteToCommandLog("Error", $"Failed to connect! {e.Message}");
                CurrentState = State.DISCONNECTED;
            }
        }

        private async Task DisconnectAsync()
        {
            if (CurrentState != State.CONNECTED) 
            {
                _logger.LogInformation("Already disconnected from RCON server");
                return;
            }
            
            _logger.LogInformation("Disconnecting from RCON server");
            await _rcon.DisconnectAsync();
            _logger.LogInformation("Successfully disconnected from RCON server");
        }

        private void StartStatusPolling()
        {
            _queryTimer = new Timer(async _ => await PollServerStatus(), null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        }

        private async Task PollServerStatus()
        {
            if (_cancellationTokenSource.Token.IsCancellationRequested)
                return;

            try
            {
                var status = await _queryService.QueryServerAsync(_serverProfile.Uri, _serverProfile.MinecraftPort);
                
                if (status.IsOnline)
                {
                    OnlinePlayerText = $"Players: {status.OnlinePlayers}/{status.MaxPlayers}";
                }
                else
                {
                    OnlinePlayerText = "Players: ?/?";
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to query server status during polling");
                OnlinePlayerText = "Players: ?/?";
            }
        }
    }
}
