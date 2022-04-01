using System;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Input;
using CoreRCON;
using Xamarin.Forms;

namespace CoralClient.ViewModel
{
    internal class RconPageViewModel : INotifyPropertyChanged
    {
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
        private string _toggleConnectionText = "ConnectAsync";

        public event PropertyChangedEventHandler PropertyChanged;

        public string ServerUriText
        {
            get => _serverUriText;
            set
            {
                _serverUriText = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ServerUriText)));
            }
        }

        public string ConnectionStatusText
        {
            get => _connectionStatusText;
            set
            {
                _connectionStatusText = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ServerUriText)));
            }
        }

        public string OnlinePlayerText
        {
            get => _onlinePlayerText;
            set
            {
                _onlinePlayerText = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ServerUriText)));
            }
        }

        public string ToggleConnectionText
        {
            get => _toggleConnectionText;
            set
            {
                _toggleConnectionText = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ServerUriText)));
            }
        }

        public ICommand ToggleConnection { get; }
        public ICommand RecentCommands { get; }

        public RconPageViewModel()
        {
            ToggleConnection = new Command(
                execute: async () =>
                {
                    switch (_connectionState)
                    {
                        case ConnectionStatus.DISCONNECTED:
                            _connectionState = await DisconnectAsync();
                            break;
                        case ConnectionStatus.CONNECTED:
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
            var host = await Dns.GetHostEntryAsync(ServerUriText);
            //ServerQuery.Info(host.AddressList.First() ); // TODO get info from user

            throw new NotImplementedException();
        }

        private async Task<ConnectionStatus> DisconnectAsync()
        {
            throw new NotImplementedException();
        }
    }
}
