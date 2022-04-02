using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using CoralClient.Model;
using CoralClient.Services;
using Xamarin.Forms;

namespace CoralClient.ViewModel
{
    public class RconPageViewModel : BaseObservableViewModel
    {

        public enum State
        {
            DISCONNECTED,
            CONNECTING,
            CONNECTED
        }

        private readonly ServerProfile _serverProfile;
        private readonly RconService _rconService;
        private string _serverNameText = "Server URI";
        private string _connectionStatusText = State.DISCONNECTED.ToString();
        private string _onlinePlayerText = "Players: ?/?";
        private string _toggleConnectionButtonText = "Connect";
        private string _commandLogText;
        private string _commandEntryText;
        private readonly StringBuilder _commandLogBuffer = new StringBuilder();
        private State _currentState;

        public event EventHandler StateChange;

        public State LastState { get; set; }

        public State CurrentState
        {
            get => _currentState;
            set
            {
                LastState = CurrentState;
                _currentState = value;
                StateChange?.Invoke(this, EventArgs.Empty);
            }
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

        public ICommand RecentCommandsCommand { get; }

        public RconPageViewModel(ServerProfile serverProfile, RconService rconService)
        {
            _serverProfile = serverProfile ?? throw new ArgumentNullException(nameof(serverProfile));
            _rconService = rconService ?? throw new ArgumentNullException(nameof(rconService));
            ServerNameText = serverProfile.ServerUriText;

            SendCommandCommand = new Command(
                execute: async () =>
                {
                    if (string.IsNullOrWhiteSpace(CommandEntryText)) return;
                    if (CurrentState != State.CONNECTED) return;

                    var result = await _rconService.SendCommandAsync(CommandEntryText);

                    CommandEntryText = string.Empty;

                    WriteToCommandLog($"Server: {result}");
                },
                canExecute: () =>
                    !string.IsNullOrWhiteSpace(CommandEntryText) && CurrentState == State.CONNECTED);

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
                },
                canExecute: () =>
                    CurrentState != State.CONNECTING);

            _rconService.OnDisconnected += (o, e) => CurrentState = State.DISCONNECTED;
            _rconService.OnConnected += (o, e) => CurrentState = State.CONNECTED;

            StateChange += OnStateChanged;
            StateChange += (sender, args) => ConnectionStatusText = CurrentState.ToString();
            StateChange += async (sender, args) =>
            {
                if (CurrentState != State.CONNECTED) return;

                try
                {
                    var status = await _rconService.GetStatusAsync(_serverProfile.MinecraftPort);

                    ServerNameText = $"{ServerNameText} ({status.Version})";
                    OnlinePlayerText = $"Players: {status.NumPlayers}/{status.MaxPlayers}";
                }
                catch (Exception e)
                {
                    WriteToCommandLog(e.Message);
                }
            };
        }

        private void OnStateChanged(object sender, EventArgs e)
        {
            switch (CurrentState)
            {
                case State.DISCONNECTED:
                    {
                        ServerNameText = _serverProfile.ServerUriText;
                        OnlinePlayerText = "Players: ?/?";
                        WriteToCommandLog("Disconnected!");
                        ToggleConnectionButtonText = "Connect";
                    }
                    break;
                case State.CONNECTING:
                    {
                        WriteToCommandLog("Connecting...");
                    }
                    break;
                case State.CONNECTED:
                    {
                        WriteToCommandLog("Connected!");
                        ToggleConnectionButtonText = "Disconnect";
                    }
                    break;
            }
        }

        private void WriteToCommandLog(string text, bool newLine = true)
        {
            var formattedText = $"[{DateTime.Now}] {text}";

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

                WriteToCommandLog($"Querying {targetAddress}:{_serverProfile.MinecraftPort}");

                await _rconService.ConnectAsync(targetAddress, _serverProfile.RconPort, _serverProfile.Password);
            }
            catch (Exception e)
            {
                await _rconService.DisconnectAsync();

                WriteToCommandLog(e.Message);
            }
        }

        private async Task DisconnectAsync()
        {
            await _rconService.DisconnectAsync();
        }
    }
}
