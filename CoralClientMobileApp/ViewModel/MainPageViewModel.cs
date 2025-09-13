using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CoralClientMobileApp.DbContext;
using CoralClientMobileApp.Model;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;

namespace CoralClientMobileApp.ViewModel
{
    public partial class MainPageViewModel : BaseObservableViewModel
    {
        private readonly ServerProfileContext _serverProfileContext;
        private Func<string, string, Task<string>>? _promptUserFuncAsync;
        private Func<ServerProfile, Task>? _showRconPageFuncAsync;

        public ObservableCollection<ServerProfile> ServerProfiles { get; }

        public MainPageViewModel(ServerProfileContext serverProfileContext)
        {
            _serverProfileContext = serverProfileContext;
            ServerProfiles = new ObservableCollection<ServerProfile>();
        }

        public async Task InitializeAsync()
        {
            try
            {
                // Initialize the database
                await _serverProfileContext.InitializeDatabaseAsync();
                
                // Load existing profiles
                await LoadServerProfilesAsync();
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error initializing database: {ex}");
            }
        }

        private async Task LoadServerProfilesAsync()
        {
            try
            {
                var profiles = await _serverProfileContext.ServerProfiles.ToListAsync();
                ServerProfiles.Clear();
                
                foreach (var profile in profiles)
                {
                    ServerProfiles.Add(profile);
                }
                
                System.Console.WriteLine($"Loaded {profiles.Count} server profiles from database");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error loading server profiles: {ex}");
            }
        }

        public void Initialize(
            Func<string, string, Task<string>> promptUserFunc,
            Func<ServerProfile, Task> showRconPageFuncAsync)
        {
            _promptUserFuncAsync = promptUserFunc;
            _showRconPageFuncAsync = showRconPageFuncAsync;
        }

        [RelayCommand]
        private async Task AddServerProfile()
        {
            try
            {
                var newProfile = await GetServerProfileAsync();
                
                if (newProfile is null) return;

                await _serverProfileContext.ServerProfiles.AddAsync(newProfile);
                await _serverProfileContext.SaveChangesAsync();
                
                ServerProfiles.Add(newProfile);
                System.Console.WriteLine($"Added server profile: {newProfile.ServerUriText}");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error adding server profile: {ex}");
            }
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
            try
            {
                var editedProfile = await GetServerProfileAsync();

                if (editedProfile is null) return;

                // Update the existing profile instead of removing and adding
                var existingProfile = await _serverProfileContext.ServerProfiles.FindAsync(serverProfile.Id);
                if (existingProfile != null)
                {
                    existingProfile.Uri = editedProfile.Uri;
                    existingProfile.MinecraftPort = editedProfile.MinecraftPort;
                    existingProfile.RconPort = editedProfile.RconPort;
                    existingProfile.Password = editedProfile.Password;
                    
                    await _serverProfileContext.SaveChangesAsync();
                    
                    // Refresh the collection
                    await LoadServerProfilesAsync();
                    System.Console.WriteLine($"Updated server profile: {existingProfile.ServerUriText}");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error editing server profile: {ex}");
            }
        }

        [RelayCommand]
        private async Task DeleteProfile(ServerProfile serverProfile)
        {
            try
            {
                _serverProfileContext.ServerProfiles.Remove(serverProfile);
                await _serverProfileContext.SaveChangesAsync();
                
                ServerProfiles.Remove(serverProfile);
                System.Console.WriteLine($"Deleted server profile: {serverProfile.ServerUriText}");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error deleting server profile: {ex}");
            }
        }

        private async Task<ServerProfile?> GetServerProfileAsync()
        {
            if (_promptUserFuncAsync == null) return null;

            var serverUri = await _promptUserFuncAsync("Server URI", "Enter the server URI or IP address.");

            if (string.IsNullOrWhiteSpace(serverUri))
            {
                return null;
            }

            var serverMinecraftPort = await _promptUserFuncAsync("Server Minecraft Port", "Enter the Minecraft port (25565).");

            if (!ushort.TryParse(serverMinecraftPort, out var serverMinecraftPortParsed))
            {
                serverMinecraftPortParsed = 25565;
            }
            
            var serverRconPort = await _promptUserFuncAsync("Server RCON Port", "Enter the RCON port (25575).");

            if (!ushort.TryParse(serverRconPort, out var serverRconPortParsed))
            {
                serverRconPortParsed = 25575;
            }

            var serverRconPassword = await _promptUserFuncAsync("Server RCON Password", "Enter the RCON password.");

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
