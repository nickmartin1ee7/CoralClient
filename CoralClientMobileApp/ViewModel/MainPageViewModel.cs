using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
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
    public partial class MainPageViewModel : BaseObservableViewModel, IDisposable
    {
        private readonly ServerProfileContext _serverProfileContext;
        private readonly ILogger<MainPageViewModel> _logger;
        private readonly MinecraftQueryService _queryService;
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly Dictionary<Guid, Timer> _serverQueryTimers = new();
        private Func<ServerProfile?, Task<ServerProfile?>>? _showServerProfileEditModalFuncAsync;
        private Func<ServerProfile, Task>? _showRconPageFuncAsync;

        public ObservableCollection<ServerProfileViewModel> ServerProfiles { get; } = [];
        
        // Separate collections for server statuses and loading states
        private readonly Dictionary<Guid, ServerStatus> _serverStatuses = new();
        private readonly Dictionary<Guid, bool> _loadingStates = new();
        private readonly Dictionary<Guid, bool> _hasCompletedFirstPoll = new();

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
                
                // Start polling for each server profile
                StartServerPolling();
                
                _logger.LogInformation("Loaded {ProfileCount} server profiles from database", profiles.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load server profiles from database");
            }
        }

        private void StartServerPolling()
        {
            _logger.LogInformation("Starting continuous polling for {ProfileCount} server profiles", ServerProfiles.Count);
            
            foreach (var profileViewModel in ServerProfiles)
            {
                StartPollingForProfile(profileViewModel.ServerProfile);
            }
        }

        private void StartPollingForProfile(ServerProfile profile)
        {
            if (_serverQueryTimers.ContainsKey(profile.Id))
            {
                _serverQueryTimers[profile.Id].Dispose();
            }

            _serverQueryTimers[profile.Id] = new Timer(
                async _ => await PollServerStatus(profile),
                null,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(5)
            );
        }

        private void StopPollingForProfile(Guid profileId)
        {
            if (_serverQueryTimers.TryGetValue(profileId, out var timer))
            {
                timer.Dispose();
                _serverQueryTimers.Remove(profileId);
            }
        }

        private async Task PollServerStatus(ServerProfile profile)
        {
            if (_cancellationTokenSource.Token.IsCancellationRequested)
                return;

            try
            {
                // Only set loading state to true for the very first poll
                if (!_hasCompletedFirstPoll.ContainsKey(profile.Id) || !_hasCompletedFirstPoll[profile.Id])
                {
                    SetLoadingState(profile.Id, true);
                    
                    // Find and notify the specific profile view model
                    var profileViewModel = ServerProfiles.FirstOrDefault(p => p.ServerProfile.Id == profile.Id);
                    profileViewModel?.NotifyAllPropertiesChanged();
                }

                var status = await _queryService.QueryServerFullAsync(profile.Uri, profile.MinecraftPort);
                SetServerStatus(profile.Id, status);
                
                // Mark as completed first poll and set loading to false only if this was the first poll
                if (!_hasCompletedFirstPoll.ContainsKey(profile.Id) || !_hasCompletedFirstPoll[profile.Id])
                {
                    _hasCompletedFirstPoll[profile.Id] = true;
                    SetLoadingState(profile.Id, false);
                }
                
                // Notify UI thread that this specific profile's status has updated
                var profileViewModelAfter = ServerProfiles.FirstOrDefault(p => p.ServerProfile.Id == profile.Id);
                profileViewModelAfter?.NotifyAllPropertiesChanged();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to query server status for {ServerUri} during polling", profile.ServerUriText);
                
                // Mark as completed first poll and set loading to false only if this was the first poll
                if (!_hasCompletedFirstPoll.ContainsKey(profile.Id) || !_hasCompletedFirstPoll[profile.Id])
                {
                    _hasCompletedFirstPoll[profile.Id] = true;
                    SetLoadingState(profile.Id, false);
                }
                
                // Find and notify the specific profile view model
                var profileViewModel = ServerProfiles.FirstOrDefault(p => p.ServerProfile.Id == profile.Id);
                profileViewModel?.NotifyAllPropertiesChanged();
            }
        }

        public void Initialize(
            Func<ServerProfile?, Task<ServerProfile?>> showServerProfileEditModalFunc,
            Func<ServerProfile, Task> showRconPageFuncAsync)
        {
            _showServerProfileEditModalFuncAsync = showServerProfileEditModalFunc;
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
                
                var newProfileViewModel = new ServerProfileViewModel(newProfile, this);
                ServerProfiles.Add(newProfileViewModel);
                
                // Start polling for the new server profile
                StartPollingForProfile(newProfile);
                
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
                
                var editedProfile = await GetServerProfileAsync(serverProfileViewModel.ServerProfile);

                if (editedProfile is null) 
                {
                    _logger.LogInformation("Server profile edit cancelled by user");
                    return;
                }

                // Update the existing profile instead of removing and adding
                var existingProfile = await _serverProfileContext.ServerProfiles.FindAsync(serverProfileViewModel.ServerProfile.Id);
                if (existingProfile != null)
                {
                    existingProfile.Name = editedProfile.Name;
                    existingProfile.Uri = editedProfile.Uri;
                    existingProfile.MinecraftPort = editedProfile.MinecraftPort;
                    existingProfile.RconPort = editedProfile.RconPort;
                    existingProfile.Password = editedProfile.Password;
                    
                    await _serverProfileContext.SaveChangesAsync();
                    
                    // Reset polling and loading states for this profile
                    StopPollingForProfile(existingProfile.Id);
                    _serverStatuses.Remove(existingProfile.Id);
                    _loadingStates.Remove(existingProfile.Id);
                    _hasCompletedFirstPoll.Remove(existingProfile.Id);
                    
                    // Refresh the collection and restart polling
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
                
                // Stop polling for this profile
                StopPollingForProfile(serverProfileViewModel.ServerProfile.Id);
                
                ServerProfiles.Remove(serverProfileViewModel);
                
                // Clean up the dictionaries
                _serverStatuses.Remove(serverProfileViewModel.ServerProfile.Id);
                _loadingStates.Remove(serverProfileViewModel.ServerProfile.Id);
                _hasCompletedFirstPoll.Remove(serverProfileViewModel.ServerProfile.Id);
                
                _logger.LogInformation("Successfully deleted server profile: {ServerUri}", serverProfileViewModel.ServerProfile.ServerUriText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete server profile: {ServerUri}", serverProfileViewModel.ServerProfile.ServerUriText);
            }
        }

        private async Task<ServerProfile?> GetServerProfileAsync(ServerProfile? existingProfile = null)
        {
            if (_showServerProfileEditModalFuncAsync == null) return null;

            return await _showServerProfileEditModalFuncAsync(existingProfile);
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            
            // Dispose all server query timers
            foreach (var timer in _serverQueryTimers.Values)
            {
                timer?.Dispose();
            }
            _serverQueryTimers.Clear();
            
            // Clear all tracking dictionaries
            _serverStatuses.Clear();
            _loadingStates.Clear();
            _hasCompletedFirstPoll.Clear();
            
            _cancellationTokenSource?.Dispose();
        }
    }
}
