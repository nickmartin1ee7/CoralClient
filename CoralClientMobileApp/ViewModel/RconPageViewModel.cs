using System;
using System.Collections.ObjectModel;
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

        public event EventHandler<CustomCommand?>? OpenEditorRequested;

        private readonly ServerProfile _serverProfile;
        private readonly RconClient _rcon;
        private readonly ILogger<RconPageViewModel> _logger;
        private readonly MinecraftQueryService _queryService;
        private readonly ICustomCommandService _customCommandService;
        private readonly IPlayerAvatarService _playerAvatarService;
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
        private string _serverVersionText = "Version: Unknown";

        [ObservableProperty]
        private string _serverMotdText = "MOTD: Unknown";

        [ObservableProperty]
        private string _toggleConnectionButtonText = "Connect";

        [ObservableProperty]
        private string _commandLogText = string.Empty;

        [ObservableProperty]
        private string _commandEntryText = string.Empty;

        [ObservableProperty]
        private bool _isSendCommandEnabled;

        [ObservableProperty]
        private string _selectedPlayerName = string.Empty;

        [ObservableProperty]
        private Player? _selectedPlayer;

        [ObservableProperty]
        private string _whitelistButtonText = "Enable Whitelist";

        // Tab visibility properties
        [ObservableProperty]
        private bool _isConsoleTabVisible = true;

        [ObservableProperty]
        private bool _isPlayersTabVisible;

        [ObservableProperty]
        private bool _isServerTabVisible;

        // Collections
        public ObservableCollection<Player> OnlinePlayers { get; } = new();
        public ObservableCollection<CustomCommand> PlayerCustomCommands { get; } = new();
        public ObservableCollection<CustomCommand> ServerCustomCommands { get; } = new();
        public ObservableCollection<CommandGroup> PlayerCommandGroups { get; } = new();
        public ObservableCollection<CommandGroup> ServerCommandGroups { get; } = new();

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

        public ServerProfile ServerProfile => _serverProfile;

        public RconPageViewModel(ServerProfile serverProfile, RconClient rcon, ILogger<RconPageViewModel> logger, MinecraftQueryService queryService, ICustomCommandService customCommandService, IPlayerAvatarService playerAvatarService)
        {
            _serverProfile = serverProfile ?? throw new ArgumentNullException(nameof(serverProfile));
            _rcon = rcon ?? throw new ArgumentNullException(nameof(rcon));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
            _customCommandService = customCommandService ?? throw new ArgumentNullException(nameof(customCommandService));
            _playerAvatarService = playerAvatarService ?? throw new ArgumentNullException(nameof(playerAvatarService));
            ServerNameText = serverProfile.ServerUriText;

            // Load custom commands
            _ = LoadCustomCommandsAsync();

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
        private async Task SendCommandAsync()
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

        // Tab Commands
        [RelayCommand]
        private void ShowConsoleTab()
        {
            SetActiveTab("Console");
        }

        [RelayCommand]
        private void ShowPlayersTab()
        {
            SetActiveTab("Players");
        }

        [RelayCommand]
        private void ShowServerTab()
        {
            SetActiveTab("Server");
        }

        [RelayCommand]
        private async Task ShowCustomTabAsync()
        {
            // Open the custom command editor for creating a new command
            await OpenCustomCommandEditorAsync();
        }

        // Player Commands
        [RelayCommand]
        private async Task KickPlayerAsync()
        {
            var playerName = GetTargetPlayerName();
            if (!string.IsNullOrEmpty(playerName))
            {
                await ExecuteRconCommandAsync($"kick {playerName}");
            }
        }

        [RelayCommand]
        private async Task BanPlayerAsync()
        {
            var playerName = GetTargetPlayerName();
            if (!string.IsNullOrEmpty(playerName))
            {
                await ExecuteRconCommandAsync($"ban {playerName}");
            }
        }

        [RelayCommand]
        private async Task OpPlayerAsync()
        {
            var playerName = GetTargetPlayerName();
            if (!string.IsNullOrEmpty(playerName))
            {
                await ExecuteRconCommandAsync($"op {playerName}");
            }
        }

        [RelayCommand]
        private async Task SetCreativeAsync()
        {
            var playerName = GetTargetPlayerName();
            if (!string.IsNullOrEmpty(playerName))
            {
                await ExecuteRconCommandAsync($"gamemode creative {playerName}");
            }
        }

        [RelayCommand]
        private async Task SetSurvivalAsync()
        {
            var playerName = GetTargetPlayerName();
            if (!string.IsNullOrEmpty(playerName))
            {
                await ExecuteRconCommandAsync($"gamemode survival {playerName}");
            }
        }

        [RelayCommand]
        private async Task SetSpectatorAsync()
        {
            var playerName = GetTargetPlayerName();
            if (!string.IsNullOrEmpty(playerName))
            {
                await ExecuteRconCommandAsync($"gamemode spectator {playerName}");
            }
        }

        // Server Commands
        [RelayCommand]
        private async Task StopServerAsync()
        {
            await ExecuteRconCommandAsync("stop");
        }

        [RelayCommand]
        private async Task SaveWorldAsync()
        {
            await ExecuteRconCommandAsync("save-all");
        }

        [RelayCommand]
        private async Task ReloadServerAsync()
        {
            await ExecuteRconCommandAsync("reload");
        }

        [RelayCommand]
        private async Task ToggleWhitelistAsync()
        {
            if (WhitelistButtonText.Contains("Enable"))
            {
                await ExecuteRconCommandAsync("whitelist on");
                WhitelistButtonText = "Disable Whitelist";
            }
            else
            {
                await ExecuteRconCommandAsync("whitelist off");
                WhitelistButtonText = "Enable Whitelist";
            }
        }

        // Weather Commands
        [RelayCommand]
        private async Task SetWeatherClearAsync()
        {
            await ExecuteRconCommandAsync("weather clear");
        }

        [RelayCommand]
        private async Task SetWeatherRainAsync()
        {
            await ExecuteRconCommandAsync("weather rain");
        }

        [RelayCommand]
        private async Task SetWeatherThunderAsync()
        {
            await ExecuteRconCommandAsync("weather thunder");
        }

        // Time Commands
        [RelayCommand]
        private async Task SetTimeDayAsync()
        {
            await ExecuteRconCommandAsync("time set day");
        }

        [RelayCommand]
        private async Task SetTimeNightAsync()
        {
            await ExecuteRconCommandAsync("time set night");
        }

        [RelayCommand]
        private async Task SetTimeNoonAsync()
        {
            await ExecuteRconCommandAsync("time set noon");
        }

        // Difficulty Commands
        [RelayCommand]
        private async Task SetDifficultyPeacefulAsync()
        {
            await ExecuteRconCommandAsync("difficulty peaceful");
        }

        [RelayCommand]
        private async Task SetDifficultyEasyAsync()
        {
            await ExecuteRconCommandAsync("difficulty easy");
        }

        [RelayCommand]
        private async Task SetDifficultyNormalAsync()
        {
            await ExecuteRconCommandAsync("difficulty normal");
        }

        [RelayCommand]
        private async Task SetDifficultyHardAsync()
        {
            await ExecuteRconCommandAsync("difficulty hard");
        }

        [RelayCommand]
        private async Task ExecuteCustomCommandAsync(CustomCommand customCommand)
        {
            if (customCommand == null) return;

            var command = customCommand.Command;
            if (customCommand.RequiresPlayerName)
            {
                var playerName = GetTargetPlayerName();
                if (string.IsNullOrEmpty(playerName))
                {
                    WriteToCommandLog("Error", "Player name required for this command");
                    return;
                }
                command = command.Replace("{player}", playerName);
            }

            await ExecuteRconCommandAsync(command);
        }

        [RelayCommand]
        private async Task EditCustomCommandAsync(CustomCommand customCommand)
        {
            if (customCommand == null) return;
            await OpenCustomCommandEditorAsync(customCommand);
        }

        [RelayCommand]
        private async Task OpenCustomCommandEditorAsync(CustomCommand? command = null)
        {
            // Raise event to request navigation to editor
            OpenEditorRequested?.Invoke(this, command);
            await Task.CompletedTask;
        }

        [RelayCommand]
        private async Task ToggleConnectionAsync()
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

        // Helper Methods
        public async Task LoadCustomCommandsAsync()
        {
            try
            {
                var allCommands = await _customCommandService.GetCommandsForServerAsync(_serverProfile.Id);
                
                PlayerCustomCommands.Clear();
                ServerCustomCommands.Clear();
                PlayerCommandGroups.Clear();
                ServerCommandGroups.Clear();
                
                // Group custom commands by category
                var playerGroups = allCommands
                    .Where(c => c.Target == CommandTarget.Player)
                    .GroupBy(c => string.IsNullOrEmpty(c.Category) ? "Custom" : c.Category)
                    .OrderBy(g => g.Key);
                    
                var serverGroups = allCommands
                    .Where(c => c.Target == CommandTarget.Server)
                    .GroupBy(c => string.IsNullOrEmpty(c.Category) ? "Custom" : c.Category)
                    .OrderBy(g => g.Key);
                
                foreach (var group in playerGroups)
                {
                    var commandGroup = new CommandGroup
                    {
                        CategoryName = group.Key,
                        Commands = new ObservableCollection<CustomCommand>(group.OrderBy(c => c.Name))
                    };
                    PlayerCommandGroups.Add(commandGroup);
                }
                
                foreach (var group in serverGroups)
                {
                    var commandGroup = new CommandGroup
                    {
                        CategoryName = group.Key,
                        Commands = new ObservableCollection<CustomCommand>(group.OrderBy(c => c.Name))
                    };
                    ServerCommandGroups.Add(commandGroup);
                }
                
                // Keep the flat collections for backward compatibility
                foreach (var command in allCommands)
                {
                    if (command.Target == CommandTarget.Player)
                    {
                        PlayerCustomCommands.Add(command);
                    }
                    else
                    {
                        ServerCustomCommands.Add(command);
                    }
                }
                
                _logger.LogInformation("Loaded {PlayerGroups} player command groups and {ServerGroups} server command groups", 
                    PlayerCommandGroups.Count, ServerCommandGroups.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load custom commands");
            }
        }
        
        private void SetActiveTab(string tabName)
        {
            // Reset all tabs
            IsConsoleTabVisible = false;
            IsPlayersTabVisible = false;
            IsServerTabVisible = false;

            // Set active tab
            switch (tabName)
            {
                case "Console":
                    IsConsoleTabVisible = true;
                    break;
                case "Players":
                    IsPlayersTabVisible = true;
                    break;
                case "Server":
                    IsServerTabVisible = true;
                    break;
            }
        }

        private async Task ExecuteRconCommandAsync(string command)
        {
            if (CurrentState != State.CONNECTED || !_rcon.IsAuthenticated)
            {
                WriteToCommandLog("Error", "Not connected or authenticated");
                return;
            }

            try
            {
                WriteToCommandLog("Client", command);
                await _rcon.SendCommandAsync(command);
                _logger.LogInformation("Executed command: {Command}", command);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to execute command: {Command}", command);
                WriteToCommandLog("Error", $"Failed to execute command: {e.Message}");
            }
        }

        private string GetTargetPlayerName()
        {
            return !string.IsNullOrEmpty(SelectedPlayerName) 
                ? SelectedPlayerName 
                : SelectedPlayer?.Name ?? string.Empty;
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

            var formattedText = $"[{DateTime.Now:HH:mm:ss}] {prefix}: {text.RemoveColorCodes()}";

            if (newLine)
            {
                _commandLogBuffer.AppendLine(formattedText);
            }
            else
            {
                _commandLogBuffer.Append(formattedText);
            }

            CommandLogText = _commandLogBuffer.ToString();
            
            // Trigger auto-scroll to bottom by notifying property changed
            OnPropertyChanged(nameof(CommandLogText));
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
            _queryTimer = new Timer(_ => _ = PollServerStatusAsync(), null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
        }

        private async Task PollServerStatusAsync()
        {
            try
            {
                if (_cancellationTokenSource.Token.IsCancellationRequested)
                    return;

                var status = await _queryService.QueryServerFullAsync(_serverProfile.Uri, _serverProfile.MinecraftPort);
                
                if (status.IsOnline)
                {
                    // Update player count
                    OnlinePlayerText = $"Players: {status.OnlinePlayers}/{status.MaxPlayers}";
                    
                    // Update server version
                    if (!string.IsNullOrEmpty(status.VersionName))
                    {
                        ServerVersionText = $"Version: {status.VersionName}";
                    }
                    
                    // Update MOTD
                    if (!string.IsNullOrEmpty(status.Motd))
                    {
                        ServerMotdText = status.Motd.RemoveColorCodes();
                    }
                    
                    // Update player list if we have player names
                    if (status.PlayerList?.Any() == true)
                    {
                        await UpdatePlayerListAsync(status.PlayerList);
                    }
                    else
                    {
                        OnlinePlayers.Clear();
                    }
                }
                else
                {
                    OnlinePlayerText = "Players: ?/?";
                    ServerVersionText = "Version: Server Offline";
                    OnlinePlayers.Clear();
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to query server status during polling");
                OnlinePlayerText = "Players: ?/?";
                ServerVersionText = "Version: Query Failed";
            }
        }

        private Task UpdatePlayerListAsync(string[] currentPlayerNames)
        {
            try
            {
                // Remove players that are no longer online
                var playersToRemove = OnlinePlayers.Where(p => !currentPlayerNames.Contains(p.Name)).ToList();
                foreach (var player in playersToRemove)
                {
                    OnlinePlayers.Remove(player);
                }

                // Add new players
                foreach (var playerName in currentPlayerNames)
                {
                    var existingPlayer = OnlinePlayers.FirstOrDefault(p => p.Name == playerName);
                    if (existingPlayer == null)
                    {
                        // Create new player
                        var newPlayer = new Player(playerName);
                        OnlinePlayers.Add(newPlayer);
                        
                        // Load avatar asynchronously (fire and forget)
                        _ = Task.Run(async () => await _playerAvatarService.LoadPlayerAvatarAsync(newPlayer));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update player list");
            }
            
            return Task.CompletedTask;
        }
    }
}
