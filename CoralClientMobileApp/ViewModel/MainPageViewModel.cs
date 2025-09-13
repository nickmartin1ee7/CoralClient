using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CoralClientMobileApp.DbContext;
using CoralClientMobileApp.Model;
using CommunityToolkit.Mvvm.Input;

namespace CoralClientMobileApp.ViewModel
{
    public partial class MainPageViewModel : BaseObservableViewModel
    {
        private readonly ServerProfileContext _serverProfileContext;
        private Func<string, string, Task<string>>? _promptUserFunc;
        private Func<ServerProfile, Task>? _showRconPageFuncAsync;

        public IList<ServerProfile> ServerProfiles { get; }

        public MainPageViewModel(ServerProfileContext serverProfileContext)
        {
            _serverProfileContext = serverProfileContext;
            ServerProfiles = new ObservableCollection<ServerProfile>(serverProfileContext.ServerProfiles.ToList());
        }

        public void SetDependencies(Func<string, string, Task<string>> promptUserFunc, Func<ServerProfile, Task> showRconPageFuncAsync)
        {
            _promptUserFunc = promptUserFunc;
            _showRconPageFuncAsync = showRconPageFuncAsync;
        }

        [RelayCommand]
        private async Task AddServerProfile()
        {
            var newProfile = await GetServerProfileAsync();
            
            if (newProfile is null) return;

            ServerProfiles.Add(newProfile);
            await _serverProfileContext.ServerProfiles.AddAsync(newProfile);
            await _serverProfileContext.SaveChangesAsync();
        }

        [RelayCommand]
        private async Task LaunchProfile(ServerProfile serverProfile)
        {
            if (_showRconPageFuncAsync != null)
                await _showRconPageFuncAsync(serverProfile);
        }

        [RelayCommand]
        private async Task EditProfile(ServerProfile serverProfile)
        {
            var editedProfile = await GetServerProfileAsync();

            if (editedProfile is null) return;

            ServerProfiles.Remove(serverProfile);
            _serverProfileContext.ServerProfiles.Remove(serverProfile);
            ServerProfiles.Add(editedProfile);
            await _serverProfileContext.ServerProfiles.AddAsync(editedProfile);
            await _serverProfileContext.SaveChangesAsync();
        }

        [RelayCommand]
        private async Task DeleteProfile(ServerProfile serverProfile)
        {
            ServerProfiles.Remove(serverProfile);
            _serverProfileContext.ServerProfiles.Remove(serverProfile);
            await _serverProfileContext.SaveChangesAsync();
        }

        private async Task<ServerProfile?> GetServerProfileAsync()
        {
            if (_promptUserFunc == null) return null;

            var serverUri = await _promptUserFunc("Server URI", "Enter the server URI or IP address.");

            if (string.IsNullOrWhiteSpace(serverUri))
            {
                return null;
            }

            var serverMinecraftPort = await _promptUserFunc("Server Minecraft Port", "Enter the Minecraft port (25565).");

            if (!ushort.TryParse(serverMinecraftPort, out var serverMinecraftPortParsed))
            {
                serverMinecraftPortParsed = 25565;
            }
            
            var serverRconPort = await _promptUserFunc("Server RCON Port", "Enter the RCON port (25575).");

            if (!ushort.TryParse(serverRconPort, out var serverRconPortParsed))
            {
                serverRconPortParsed = 25575;
            }

            var serverRconPassword = await _promptUserFunc("Server RCON Password", "Enter the RCON password.");

            if (string.IsNullOrWhiteSpace(serverRconPassword))
            {
                return null;
            }

            return new ServerProfile
            {
                Uri = serverUri.ToLower(),
                MinecraftPort = serverMinecraftPortParsed,
                RconPort = serverRconPortParsed,
                Password = serverRconPassword
            };
        }
    }
}
