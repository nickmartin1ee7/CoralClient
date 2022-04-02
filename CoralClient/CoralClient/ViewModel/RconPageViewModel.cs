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
        
        public ICommand ToggleConnectionCommand { get; }

        public ICommand RecentCommandsCommand { get; }

        public RconPageViewModel(ServerProfile serverProfile, RconService rconService)
        {
            _serverProfile = serverProfile ?? throw new ArgumentNullException(nameof(serverProfile));
            _rconService = rconService ?? throw new ArgumentNullException(nameof(rconService));
            ServerNameText = serverProfile.ServerUriText;

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
                
                _rconService.OnDisconnected += (o, e) => CurrentState = State.DISCONNECTED;
                _rconService.OnConnected += (o, e) => CurrentState = State.CONNECTED;
                
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
