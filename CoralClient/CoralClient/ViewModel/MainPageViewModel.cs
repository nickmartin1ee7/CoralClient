using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Security;
using System.Threading.Tasks;
using System.Windows.Input;
using CoralClient.Model;
using Xamarin.Forms;

namespace CoralClient.ViewModel
{
    public class MainPageViewModel : BaseObservableViewModel
    {
        private Func<string, string, Task<string>> _promptUserFunc;
        private ServerProfile _selectedItem;

        public ServerProfile SelectedItem
        {
            get => _selectedItem;
            set => SetProperty(ref _selectedItem, value);
        }

        public IList<ServerProfile> ServerProfiles { get; } = new ObservableCollection<ServerProfile>
        {
            new ServerProfile
            {
                Uri = "pi.hole",
                MinecraftPort = 25565
            }
        };

        public ICommand AddServerProfileCommand { get; }

        public ICommand SelectionChangeCommand { get; }

        public MainPageViewModel(Func<string, string, Task<string>> promptUserFunc, Func<ServerProfile, Task> showRconPageFuncAsync)
        {
            _promptUserFunc = promptUserFunc;

            AddServerProfileCommand = new Command(execute: async () =>
                await AddServerProfileAsync());

            SelectionChangeCommand = new Command(execute: async () =>
                await showRconPageFuncAsync(SelectedItem));
        }

        private async Task AddServerProfileAsync()
        {
            var serverUri = await _promptUserFunc("Server URI", "Enter the server URI or IP address.");

            if (string.IsNullOrWhiteSpace(serverUri))
            {
                return;
            }

            var serverMinecraftPort = await _promptUserFunc("Server Minecraft Port", "Enter the Minecraft port (25565).");

            if (string.IsNullOrWhiteSpace(serverMinecraftPort))
            {
                serverMinecraftPort = "25565";
            }
            
            var serverRconPort = await _promptUserFunc("Server RCON Port", "Enter the RCON port (25575).");

            if (string.IsNullOrWhiteSpace(serverRconPort))
            {
                serverRconPort = "25575";
            }

            var serverRconPassword = await _promptUserFunc("Server RCON Password", "Enter the RCON password.");

            if (string.IsNullOrWhiteSpace(serverRconPassword))
            {
                return;
            }

            ServerProfiles.Add(new ServerProfile
            {
                Uri = serverUri,
                MinecraftPort = ushort.Parse(serverMinecraftPort),
                RconPort = ushort.Parse(serverRconPort),
                Password = serverRconPassword
            });
        }
    }
}
