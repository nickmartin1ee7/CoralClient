using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using CoralClient.Model;
using CoralClient.View;
using Xamarin.Forms;

namespace CoralClient.ViewModel
{
    internal class MainPageViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<ServerProfile> ServerProfiles { get; set; } = new ObservableCollection<ServerProfile>();

        public ICommand AddServerProfile { get; }
        public ICommand SelectionChange { get; }

        public MainPageViewModel(Func<string, string, Task<string>> promptUserFunc)
        {
            AddServerProfile = new Command(
                execute: async () =>
                {
                    var serverUri = await promptUserFunc("Server URI", "Enter the server URI or IP address.");
                    var serverMinecraftPort = await promptUserFunc("Server Minecraft Port", "Enter the Minecraft port (25565).");
                    var serverRconPort = await promptUserFunc("Server RCON Port", "Enter the RCON port (25575).");

                    if (string.IsNullOrWhiteSpace(serverUri))
                    {
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(serverMinecraftPort))
                    {
                        serverMinecraftPort = "25565";
                    }

                    if (string.IsNullOrWhiteSpace(serverRconPort))
                    {
                        serverRconPort = "25575";
                    }

                    ServerProfiles.Add(new ServerProfile
                    {
                        Uri = serverUri,
                        MinecraftPort = int.Parse(serverMinecraftPort),
                        RconPort = int.Parse(serverRconPort)
                    });
                });

            SelectionChange = new Command(async (serverProfile) =>
            {
                await Shell.Current.GoToAsync(nameof(RconPage));
            });
        }
    }
}
