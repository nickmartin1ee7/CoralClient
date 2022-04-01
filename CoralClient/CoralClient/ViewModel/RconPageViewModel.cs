using System;
using System.Linq;
using System.Net;
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

        public enum ConnectionStatus
        {
            DISCONNECTED,
            CONNECTING,
            CONNECTED
        }

        private ConnectionStatus _connectionState;
        private string _serverUriText = "Server URI";
        private string _connectionStatusText = "Disconnected";
        private string _onlinePlayerText = "Players: 0/20";
        private string _toggleConnectionText = "Connect";

        public string ServerUriText
        {
            get => _serverUriText;
            set => SetProperty(ref _serverUriText, value);
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

        public string ToggleConnectionText
        {
            get => _toggleConnectionText;
            set => SetProperty(ref _toggleConnectionText, value);
        }

        public ICommand ToggleConnectionCommand { get; }

        public ICommand RecentCommandsCommand { get; }

        public RconPageViewModel(ServerProfile serverProfile)
        {
            _serverProfile = serverProfile;

            ServerUriText = serverProfile.ServerUriText;

            ToggleConnectionCommand = new Command(
                execute: async () =>
                {
                    switch (_connectionState)
                    {
                        case ConnectionStatus.CONNECTED:
                            _connectionState = await DisconnectAsync();
                            break;
                        case ConnectionStatus.DISCONNECTED:
                            _connectionState = await ConnectAsync();
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                },
                canExecute: () =>
                    _connectionState != ConnectionStatus.CONNECTING);
        }

        private async Task<ConnectionStatus> ConnectAsync()
        {
            _connectionState = ConnectionStatus.CONNECTING;

            var host = await Dns.GetHostEntryAsync(_serverProfile.Uri);
            var result = await ServerQuery.Info(host.AddressList.First(), _serverProfile.MinecraftPort, ServerQuery.ServerType.Minecraft) as MinecraftQueryInfo;

            return ConnectionStatus.CONNECTED;
        }

        private async Task<ConnectionStatus> DisconnectAsync()
        {
            return ConnectionStatus.DISCONNECTED;
        }
    }
}
