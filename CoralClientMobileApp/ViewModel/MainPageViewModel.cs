using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CoralClientMobileApp.DbContext;
using CoralClientMobileApp.Model;
using CoralClientMobileApp.Services;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CoralClientMobileApp.ViewModel
{
    public partial class MainPageViewModel : BaseObservableViewModel
    {
        private readonly ServerProfileContext _serverProfileContext;
        private readonly ILogger<MainPageViewModel> _logger;
        private readonly MinecraftQueryService _queryService;
        private Func<string, string, Task<string>>? _promptUserFuncAsync;
        private Func<ServerProfile, Task>? _showRconPageFuncAsync;

        public ObservableCollection<ServerProfileViewModel> ServerProfiles { get; } = [];
        
        // Separate collections for server statuses and loading states
        private readonly Dictionary<Guid, ServerStatus> _serverStatuses = new();
        private readonly Dictionary<Guid, bool> _loadingStates = new();

        [ObservableProperty]        
        private bool _isLoadingAllStatuses;

        public MainPageViewModel(ServerProfileContext serverProfileContext, ILogger<MainPageViewModel> logger, MinecraftQueryService queryService)
        {
            _serverProfileContext = serverProfileContext;
            _logger = logger;
            _queryService = queryService;
        }

        // Helper methods for managing server statuses and loading states
        public ServerStatus? GetServerStatus(Guid profileId)
        {
            return _serverStatuses.TryGetValue(profileId, out var status) ? status : null;
        }

        public bool IsProfileLoading(Guid profileId)
        {
            return _loadingStates.TryGetValue(profileId, out var isLoading) && isLoading;
        }

        private void SetServerStatus(Guid profileId, ServerStatus status)
        {
            _serverStatuses[profileId] = status;
        }

        private void SetLoadingState(Guid profileId, bool isLoading)
        {
            _loadingStates[profileId] = isLoading;
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
                    ServerProfiles.Add(new ServerProfileViewModel(profile, this));
                }
                
                // Load server statuses for all profiles in background without blocking
                _ = LoadServerStatusesAsync();
                
                _logger.LogInformation("Loaded {ProfileCount} server profiles from database", profiles.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load server profiles from database");
            }
        }

        private async Task LoadServerStatusesAsync()
        {
            try
            {
                IsLoadingAllStatuses = true;
                _logger.LogInformation("Starting to load server statuses for {ProfileCount} profiles", ServerProfiles.Count);
                
                var tasks = ServerProfiles.Select(async profileViewModel =>
                {
                    try
                    {
                        SetLoadingState(profileViewModel.ServerProfile.Id, true);
                        profileViewModel.NotifyAllPropertiesChanged();
                        var status = await _queryService.QueryServerAsync(profileViewModel.ServerProfile.Uri, profileViewModel.ServerProfile.MinecraftPort);
                        SetServerStatus(profileViewModel.ServerProfile.Id, status);
                        SetLoadingState(profileViewModel.ServerProfile.Id, false);
                        profileViewModel.NotifyAllPropertiesChanged();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to query server status for {ServerUri}", profileViewModel.ServerProfile.ServerUriText);
                        SetLoadingState(profileViewModel.ServerProfile.Id, false);
                        profileViewModel.NotifyAllPropertiesChanged();
                    }
                });

                await Task.WhenAll(tasks);
                
                IsLoadingAllStatuses = false;
                _logger.LogInformation("Finished loading server statuses for all profiles");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load server statuses");
                IsLoadingAllStatuses = false;
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
                
                ServerProfiles.Add(new ServerProfileViewModel(newProfile, this));
                
                _logger.LogInformation("Successfully added server profile: {ServerUri}", newProfile.ServerUriText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add server profile");
            }
        }

        [RelayCommand]
        private async Task LaunchProfile(ServerProfileViewModel serverProfileViewModel)
        {
            _logger.LogInformation("Launching RCON connection to {ServerUri}", serverProfileViewModel.ServerProfile.ServerUriText);
            
            if (_showRconPageFuncAsync != null)
                await _showRconPageFuncAsync(serverProfileViewModel.ServerProfile);
        }

        [RelayCommand]
        private async Task EditProfile(ServerProfileViewModel serverProfileViewModel)
        {
            try
            {
                _logger.LogInformation("Editing server profile: {ServerUri}", serverProfileViewModel.ServerProfile.ServerUriText);
                
                var editedProfile = await GetServerProfileAsync();

                if (editedProfile is null) 
                {
                    _logger.LogInformation("Server profile edit cancelled by user");
                    return;
                }

                // Update the existing profile instead of removing and adding
                var existingProfile = await _serverProfileContext.ServerProfiles.FindAsync(serverProfileViewModel.ServerProfile.Id);
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
                _logger.LogError(ex, "Failed to edit server profile: {ServerUri}", serverProfileViewModel.ServerProfile.ServerUriText);
            }
        }

        [RelayCommand]
        private async Task DeleteProfile(ServerProfileViewModel serverProfileViewModel)
        {
            try
            {
                _logger.LogInformation("Deleting server profile: {ServerUri}", serverProfileViewModel.ServerProfile.ServerUriText);
                
                _serverProfileContext.ServerProfiles.Remove(serverProfileViewModel.ServerProfile);
                await _serverProfileContext.SaveChangesAsync();
                
                ServerProfiles.Remove(serverProfileViewModel);
                
                // Clean up the dictionaries
                _serverStatuses.Remove(serverProfileViewModel.ServerProfile.Id);
                _loadingStates.Remove(serverProfileViewModel.ServerProfile.Id);
                
                _logger.LogInformation("Successfully deleted server profile: {ServerUri}", serverProfileViewModel.ServerProfile.ServerUriText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete server profile: {ServerUri}", serverProfileViewModel.ServerProfile.ServerUriText);
            }
        }

        [RelayCommand]
        private async Task RefreshServerStatuses()
        {
            // Don't start another refresh if already loading
            if (IsLoadingAllStatuses)
            {
                _logger.LogInformation("Server status refresh already in progress, skipping");
                return;
            }
            
            await LoadServerStatusesAsync();
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
