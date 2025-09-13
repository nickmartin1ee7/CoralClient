using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CoralClientMobileApp.DbContext;
using CoralClientMobileApp.Model;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoralClientMobileApp.ViewModel
{
    public partial class MainPageViewModel : BaseObservableViewModel
    {
        private readonly ServerProfileContext _serverProfileContext;
        private readonly ILogger<MainPageViewModel> _logger;
        private Func<string, string, Task<string>>? _promptUserFuncAsync;
        private Func<ServerProfile, Task>? _showRconPageFuncAsync;

        public ObservableCollection<ServerProfile> ServerProfiles { get; }

        public MainPageViewModel(ServerProfileContext serverProfileContext, ILogger<MainPageViewModel> logger)
        {
            _serverProfileContext = serverProfileContext;
            _logger = logger;
            ServerProfiles = new ObservableCollection<ServerProfile>();
        }

        public async Task InitializeAsync()
        {
            try
            {
                _logger.LogInformation("Initializing MainPageViewModel");
                
                // Initialize the database
                await _serverProfileContext.InitializeDatabaseAsync();
                
                // Load existing profiles
                await LoadServerProfilesAsync();
                
                _logger.LogInformation("MainPageViewModel initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize MainPageViewModel");
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
                
                _logger.LogInformation("Loaded {ProfileCount} server profiles from database", profiles.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load server profiles from database");
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
                _logger.LogInformation("Adding new server profile");
                
                var newProfile = await GetServerProfileAsync();
                
                if (newProfile is null) 
                {
                    _logger.LogInformation("Server profile creation cancelled by user");
                    return;
                }

                await _serverProfileContext.ServerProfiles.AddAsync(newProfile);
                await _serverProfileContext.SaveChangesAsync();
                
                ServerProfiles.Add(newProfile);
                
                _logger.LogInformation("Successfully added server profile: {ServerUri}", newProfile.ServerUriText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add server profile");
            }
        }

        [RelayCommand]
        private async Task LaunchProfile(ServerProfile serverProfile)
        {
            _logger.LogInformation("Launching RCON connection to {ServerUri}", serverProfile.ServerUriText);
            
            if (_showRconPageFuncAsync != null)
                await _showRconPageFuncAsync(serverProfile);
        }

        [RelayCommand]
        private async Task EditProfile(ServerProfile serverProfile)
        {
            try
            {
                _logger.LogInformation("Editing server profile: {ServerUri}", serverProfile.ServerUriText);
                
                var editedProfile = await GetServerProfileAsync();

                if (editedProfile is null) 
                {
                    _logger.LogInformation("Server profile edit cancelled by user");
                    return;
                }

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
                    
                    _logger.LogInformation("Successfully updated server profile: {ServerUri}", existingProfile.ServerUriText);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to edit server profile: {ServerUri}", serverProfile.ServerUriText);
            }
        }

        [RelayCommand]
        private async Task DeleteProfile(ServerProfile serverProfile)
        {
            try
            {
                _logger.LogInformation("Deleting server profile: {ServerUri}", serverProfile.ServerUriText);
                
                _serverProfileContext.ServerProfiles.Remove(serverProfile);
                await _serverProfileContext.SaveChangesAsync();
                
                ServerProfiles.Remove(serverProfile);
                
                _logger.LogInformation("Successfully deleted server profile: {ServerUri}", serverProfile.ServerUriText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete server profile: {ServerUri}", serverProfile.ServerUriText);
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
