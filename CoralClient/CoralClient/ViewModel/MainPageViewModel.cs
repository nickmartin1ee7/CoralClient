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

        public IList<ServerProfile> ServerProfiles { get; } = new ObservableCollection<ServerProfile>
        {
            new ServerProfile
            {
                Uri = "pi.hole",
                MinecraftPort = 25565,
                RconPort = 25575,
                Password = "x"
            }
        };

        public ICommand AddServerProfileCommand { get; }

        public ICommand LaunchProfileCommand { get; }

        public ICommand EditProfileCommand { get; }

        public MainPageViewModel(Func<string, string, Task<string>> promptUserFunc, Func<ServerProfile, Task> showRconPageFuncAsync)
        {
            _promptUserFunc = promptUserFunc;

            AddServerProfileCommand = new Command(execute: async () =>
            {
                var newProfile = await GetServerProfileAsync();
                
                if (newProfile is null) return;

                ServerProfiles.Add(newProfile);
            });

            LaunchProfileCommand = new Command(execute: async (serverProfile) =>
                await showRconPageFuncAsync((ServerProfile)serverProfile));

            EditProfileCommand = new Command(execute: async (serverProfile) =>
            {
                var editedProfile = await GetServerProfileAsync();

                if (editedProfile is null) return;

                ServerProfiles.Remove((ServerProfile)serverProfile);
                ServerProfiles.Add(editedProfile);
            });
        }

        private async Task<ServerProfile> GetServerProfileAsync()
        {
            var serverUri = await _promptUserFunc("Server URI", "Enter the server URI or IP address.");

            if (string.IsNullOrWhiteSpace(serverUri))
            {
                return null;
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
                return null;
            }

            return new ServerProfile
            {
                Uri = serverUri,
                MinecraftPort = ushort.Parse(serverMinecraftPort),
                RconPort = ushort.Parse(serverRconPort),
                Password = serverRconPassword
            };
        }
    }
}
