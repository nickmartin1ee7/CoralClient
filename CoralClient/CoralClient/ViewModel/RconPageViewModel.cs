using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using CoralClient.Model;
using CoreRCON;
using CoreRCON.PacketFormats;
using Xamarin.Forms;

namespace CoralClient.ViewModel
{
    public class RconPageViewModel : BaseObservableViewModel
    {
        private readonly ServerProfile _serverProfile;

        public enum State
        {
            DISCONNECTED,
            CONNECTING,
            CONNECTED
        }

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
        
        public ICommand ToggleConnectionCommand { get; }

        public ICommand RecentCommandsCommand { get; }

        public RconPageViewModel(ServerProfile serverProfile)
        {
            _serverProfile = serverProfile;

            ServerNameText = serverProfile.ServerUriText;

            ToggleConnectionCommand = new Command(
                execute: async () =>
                {
                    switch (CurrentState)
                    {
                        case State.CONNECTED:
                            CurrentState = await DisconnectAsync();
                            break;
                        case State.DISCONNECTED:
                            CurrentState = await ConnectAsync();
                            break;
                    }
                },
                canExecute: () =>
                    CurrentState != State.CONNECTING);

            StateChange += OnStateChanged;
            StateChange += (sender, args) => ConnectionStatusText = CurrentState.ToString();
        }

        private void OnStateChanged(object sender, EventArgs e)
        {
            switch (CurrentState)
            {
                case State.DISCONNECTED:
                    {
                        OnlinePlayerText = "Players: ?/?";
                        WriteToCommandLog("Disconnected...");
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

        private async Task<State> ConnectAsync()
        {
            CurrentState = State.CONNECTING;

            try
            {
                var host = await Dns.GetHostEntryAsync(_serverProfile.Uri);
                var targetAddress = host.AddressList.First();
                WriteToCommandLog($"Querying {targetAddress}...");
                var result = await ServerQuery.Info(targetAddress,
                    _serverProfile.MinecraftPort,
                    ServerQuery.ServerType.Minecraft) as MinecraftQueryInfo;
                WriteToCommandLog($"Connected to Minecraft ({result.Version})");
                OnlinePlayerText = $"Players: {result.NumPlayers}/{result.MaxPlayers}";
            }
            catch (Exception e)
            {
                WriteToCommandLog(e.Message);
                return State.DISCONNECTED;
            }

            return State.CONNECTED;
        }

        private async Task<State> DisconnectAsync()
        {
            return State.DISCONNECTED;
        }
    }
}
