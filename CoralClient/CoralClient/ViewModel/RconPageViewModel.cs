using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using CoralClient.Helpers;
using CoralClient.Model;
using MinecraftRcon;
using Xamarin.Forms;

namespace CoralClient.ViewModel
{
    public class RconPageViewModel : BaseObservableViewModel, IDisposable
    {

        public enum State
        {
            DISCONNECTED,
            CONNECTING,
            CONNECTED
        }

        private readonly ServerProfile _serverProfile;
        private readonly RconClient _rcon;
        private string _serverNameText = "Server URI";
        private string _connectionStatusText = State.DISCONNECTED.ToString();
        private string _onlinePlayerText = "Players: ?/?";
        private string _toggleConnectionButtonText = "Connect";
        private string _commandLogText;
        private string _commandEntryText;
        private readonly StringBuilder _commandLogBuffer = new StringBuilder();
        private State _currentState;
        private bool _isSendCommandEnabled;

        public event EventHandler StateChange;

        public State CurrentState
        {
            get => _currentState;
            set
            {
                _currentState = value;
                StateChange?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool IsSendCommandEnabled
        {
            get => _isSendCommandEnabled;
            set => SetProperty(ref _isSendCommandEnabled, value);
        }

        public string ServerNameText
        {
            get => _serverNameText;
            set => SetProperty(ref _serverNameText, value);
        }

        public string ConnectionStatusText
        {
            get => _connectionStatusText;
            set => SetProperty(ref _connectionStatusText, value);
        }

        public string OnlinePlayerText
        {
            get => _onlinePlayerText;
            set => SetProperty(ref _onlinePlayerText, value);
        }

        public string CommandLogText
        {
            get => _commandLogText;
            set => SetProperty(ref _commandLogText, value);
        }

        public string CommandEntryText
        {
            get => _commandEntryText;
            set => SetProperty(ref _commandEntryText, value);
        }

        public string ToggleConnectionButtonText
        {
            get => _toggleConnectionButtonText;
            set => SetProperty(ref _toggleConnectionButtonText, value);
        }

        public ICommand SendCommandCommand { get; }

        public ICommand ToggleConnectionCommand { get; }

        public ICommand RefreshCommand { get; }

        public RconPageViewModel(ServerProfile serverProfile, RconClient rcon)
        {
            _serverProfile = serverProfile ?? throw new ArgumentNullException(nameof(serverProfile));
            _rcon = rcon ?? throw new ArgumentNullException(nameof(rcon));
            ServerNameText = serverProfile.ServerUriText;

            SendCommandCommand = new Command(
                execute: async () =>
                {
                    if (CurrentState != State.CONNECTED) return;
                    if (string.IsNullOrWhiteSpace(CommandEntryText)) return;

                    WriteToCommandLog($"Client: {CommandEntryText}");

                    try
                    {
                        await _rcon.SendCommandAsync(CommandEntryText);
                        CommandEntryText = string.Empty;
                    }
                    catch (Exception e)
                    {
                        WriteToCommandLog($"Failed to send command! {e.Message}");
                    }
                });

            ToggleConnectionCommand = new Command(
                execute: async () =>
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
                });

            RefreshCommand = new Command(
                execute: async () =>
                {
                    if (CurrentState != State.CONNECTED) return;

                    try
                    {
                        await GetServerInfo();
                        WriteToCommandLog("Refreshed server info.");
                    }
                    catch (Exception e)
                    {
                        WriteToCommandLog($"Failed to refresh server info! {e.Message}");
                        await ConnectAsync();
                    }
                });

            _rcon.Disconnected += (o, e) => CurrentState = State.DISCONNECTED;
            _rcon.Connected += (o, e) => CurrentState = State.CONNECTED;
            _rcon.MessageReceived += (o, e) =>
                WriteToCommandLog($"Server: {e.Body}");
            _rcon.MessageReceived += (o, e) => // Auto update player count on any list commands
            {
                var playerText = e.Body.RemoveColorCodes();

                var pCountStartIdx = playerText.IndexOf("There are ") + "There are ".Length;
                var pCountEndLen = playerText.IndexOf(" out of") - pCountStartIdx;
                var maxCountStartIdx = playerText.IndexOf("maximum ") + "maximum ".Length;
                var maxCountEndLen = playerText.IndexOf(" players online.") - maxCountStartIdx;

                if (pCountStartIdx < 0 || pCountEndLen < 0 || maxCountStartIdx < 0 || maxCountEndLen < 0)
                {
                    return;
                }

                var currentPlayers = string.Join(string.Empty, playerText
                    .Skip(pCountStartIdx)
                    .Take(pCountEndLen));

                var maxPlayers = string.Join(string.Empty, playerText
                    .Skip(maxCountStartIdx)
                    .Take(maxCountEndLen));

                OnlinePlayerText = $"Players: {currentPlayers}/{maxPlayers}";
            };

            StateChange += UiStateChangeLogic;
            StateChange += ConnectedLogic;
        }

        public void Dispose()
        {
            _rcon?.Dispose();
        }

        private async void ConnectedLogic(object sender, EventArgs args)
        {
            if (CurrentState != State.CONNECTED) return;

            try
            {
                await _rcon.AuthenticateAsync(_serverProfile.Password);

                WriteToCommandLog("Authenticated successfully.");

                await GetServerInfo();
            }
            catch (Exception e)
            {
                WriteToCommandLog($"Failed to authenticate! {e.Message}");
            }
        }
        
        private async Task GetServerInfo()
        {
            if (CurrentState != State.CONNECTED) return;
            await _rcon.SendCommandAsync("list");
        }

        private void UiStateChangeLogic(object sender, EventArgs e)
        {
            ConnectionStatusText = CurrentState.ToString();

            switch (CurrentState)
            {
                case State.DISCONNECTED:
                    {
                        ServerNameText = _serverProfile.ServerUriText;
                        OnlinePlayerText = "Players: ?/?";
                        WriteToCommandLog("Disconnected.");
                        IsSendCommandEnabled = false;
                        ToggleConnectionButtonText = "Connect";
                    }
                    break;
                case State.CONNECTED:
                    {
                        WriteToCommandLog("Connection established.");
                        IsSendCommandEnabled = true;
                        ToggleConnectionButtonText = "Disconnect";
                    }
                    break;
            }
        }

        private void WriteToCommandLog(string text, bool newLine = true)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            var formattedText = $"[{DateTime.Now}] {text.RemoveColorCodes()}";

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

                WriteToCommandLog($"Establishing connection to {targetAddress}:{_serverProfile.MinecraftPort}");

                await _rcon.ConnectAsync(targetAddress.ToString(), _serverProfile.RconPort);
            }
            catch (Exception e)
            {
                await DisconnectAsync();
                WriteToCommandLog(e.Message);
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
