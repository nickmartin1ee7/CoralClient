using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CoralClientMobileApp.Helpers;
using CoralClientMobileApp.Model;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MinecraftRcon;

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
        private readonly StringBuilder _commandLogBuffer = new StringBuilder();
        private State _currentState;

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

        public RconPageViewModel(ServerProfile serverProfile, RconClient rcon)
        {
            _serverProfile = serverProfile ?? throw new ArgumentNullException(nameof(serverProfile));
            _rcon = rcon ?? throw new ArgumentNullException(nameof(rcon));
            ServerNameText = serverProfile.ServerUriText;

            _rcon.Disconnected += (o, e) => CurrentState = State.DISCONNECTED;
            _rcon.Connected += (o, e) => CurrentState = State.CONNECTED;
            _rcon.MessageReceived += (o, e) =>
                WriteToCommandLog("Server", e.Body);
            _rcon.MessageReceived += (o, e) => // Auto update player count on any list commands
            {
                if (string.IsNullOrWhiteSpace(e.Body))
                {
                    return;
                }

                var playerText = e.Body.RemoveColorCodes();

                // Ex: There are 0 of a max of 20 players online:
                int currentPlayers = 0;
                int maxPlayers = 0;

                var match = Regex.Match(playerText, $"There are (?<{nameof(currentPlayers)}>\\d+) of a max of (?<{nameof(maxPlayers)}>\\d+) players online:");

                if (!match.Success
                    || match.Groups.Count == 0)
                {
                    return;
                }

                var currentPlayersGroup = match.Groups[nameof(currentPlayers)];
                var maxPlayersGroup = match.Groups[nameof(maxPlayers)];

                int.TryParse(currentPlayersGroup?.Value, out currentPlayers);
                int.TryParse(maxPlayersGroup?.Value, out maxPlayers);

                OnlinePlayerText = $"Players: {currentPlayers}/{maxPlayers}";
            };

            StateChange += UiStateChangeLogic;
            StateChange += ConnectedLogic;
        }

        [RelayCommand]
        private async Task SendCommand()
        {
            if (CurrentState != State.CONNECTED) return;
            if (string.IsNullOrWhiteSpace(CommandEntryText)) return;

            WriteToCommandLog("Client", CommandEntryText);

            try
            {
                await _rcon.SendCommandAsync(CommandEntryText);
                CommandEntryText = string.Empty;
            }
            catch (Exception e)
            {
                WriteToCommandLog("Error", $"Failed to send command! {e.Message}");
            }
        }

        [RelayCommand]
        private async Task ToggleConnection()
        {
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

        [RelayCommand]
        private async Task Refresh()
        {
            if (CurrentState != State.CONNECTED) return;

            try
            {
                await GetServerInfo();
                WriteToCommandLog("Info", "Refreshed server info");
            }
            catch (Exception e)
            {
                WriteToCommandLog("Error", $"Failed to refresh server info! {e.Message}");
                await ConnectAsync();
            }
        }

        public void Dispose()
        {
            _rcon?.Dispose();
        }

        private async void ConnectedLogic(object? sender, EventArgs args)
        {
            if (CurrentState != State.CONNECTED) return;

            try
            {
                await _rcon.AuthenticateAsync(_serverProfile.Password);

                WriteToCommandLog("Info", "Authenticated successfully");

                await GetServerInfo();
            }
            catch (Exception e)
            {
                WriteToCommandLog("Error", $"Failed to authenticate! {e.Message}");
            }
        }

        private async Task GetServerInfo()
        {
            if (CurrentState != State.CONNECTED) return;
            await _rcon.SendCommandAsync("list");
        }

        private void UiStateChangeLogic(object? sender, EventArgs e)
        {
            ConnectionStatusText = CurrentState.ToString();

            switch (CurrentState)
            {
                case State.DISCONNECTED:
                {
                    ServerNameText = _serverProfile.ServerUriText;
                    OnlinePlayerText = "Players: ?/?";
                    WriteToCommandLog("Info", "Disconnected");
                    IsSendCommandEnabled = false;
                    ToggleConnectionButtonText = "Connect";
                }
                break;
                case State.CONNECTED:
                {
                    WriteToCommandLog("Info", "Connection established");
                    IsSendCommandEnabled = true;
                    ToggleConnectionButtonText = "Disconnect";
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

            try
            {
                var host = await Dns.GetHostEntryAsync(_serverProfile.Uri);
                var targetAddress = host.AddressList.First();

                WriteToCommandLog("Info", $"Establishing connection to {targetAddress}:{_serverProfile.MinecraftPort}");

                await _rcon.ConnectAsync(targetAddress.ToString(), _serverProfile.RconPort);
            }
            catch (Exception e)
            {
                await DisconnectAsync();
                WriteToCommandLog("Error", $"Failed to connect! {e.Message}");
                CurrentState = State.DISCONNECTED;
            }
        }

        private async Task DisconnectAsync()
        {
            if (CurrentState != State.CONNECTED) return;
            await _rcon.DisconnectAsync();
        }
    }
}
