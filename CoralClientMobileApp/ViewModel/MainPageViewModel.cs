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
        private readonly SynchronizationContext? _syncContext;
        private Func<ServerProfile?, Task<ServerProfile?>>? _showServerProfileEditModalFuncAsync;
        private Func<ServerProfile, Task>? _showRconPageFuncAsync;

        public ObservableCollection<ServerProfileViewModel> ServerProfiles { get; } = [];
        
        // Separate collections for server statuses and loading states
        private readonly Dictionary<Guid, ServerStatus> _serverStatuses = new();
        private readonly Dictionary<Guid, bool> _loadingStates = new();
        private readonly Dictionary<Guid, bool> _hasCompletedFirstPoll = new();
        private DateTime _lastSortTime = DateTime.MinValue;
        private readonly TimeSpan _sortCooldown = TimeSpan.FromSeconds(1); // Prevent sorting more than once per second

        public MainPageViewModel(ServerProfileContext serverProfileContext, ILogger<MainPageViewModel> logger, MinecraftQueryService queryService)
        {
            _serverProfileContext = serverProfileContext;
            _logger = logger;
            _queryService = queryService;
            _syncContext = SynchronizationContext.Current;
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
            
            // Sort profiles after status update to keep online servers at the top
            SortServerProfiles();
        }

        private void SetLoadingState(Guid profileId, bool isLoading)
        {
            _loadingStates[profileId] = isLoading;
        }

        private void SortServerProfiles(bool force = false)
        {
            // Rate limit sorting to prevent excessive reordering (unless forced)
            var now = DateTime.UtcNow;
            if (!force && now - _lastSortTime < _sortCooldown)
                return;

            // Sort profiles with online servers first, then by name
            var sortedProfiles = ServerProfiles
                .OrderByDescending(p => p.IsOnline)
                .ThenBy(p => p.ServerProfile.Name)
                .ToList();

            // Only update if the order has actually changed
            bool hasOrderChanged = false;
            for (int i = 0; i < sortedProfiles.Count; i++)
            {
                if (i >= ServerProfiles.Count || !ReferenceEquals(ServerProfiles[i], sortedProfiles[i]))
                {
                    hasOrderChanged = true;
                    break;
                }
            }

            if (hasOrderChanged)
            {
                _lastSortTime = now;
                
                // Dispatch the collection reordering to the UI thread
                _syncContext?.Post(_ =>
                {
                    // Clear and re-add in the new order
                    ServerProfiles.Clear();
                    foreach (var profile in sortedProfiles)
                    {
                        ServerProfiles.Add(profile);
                    }
                    
                    _logger.LogDebug("Reordered server profiles - online servers moved to top");
                }, null);
            }
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
                
                // Sort profiles initially (offline servers will be at the bottom initially)
                SortServerProfiles(force: true);
                
                // Don't start polling here - it will be started when the page appears
                
                _logger.LogInformation("Loaded {ProfileCount} server profiles from database", profiles.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load server profiles from database");
            }
        }

        public void StartPolling()
        {
            _logger.LogInformation("Starting server polling");
            StartServerPolling();
        }

        public void StopPolling()
        {
            _logger.LogInformation("Stopping server polling");
            StopAllPolling();
        }

        private void StopAllPolling()
        {
            foreach (var timer in _serverQueryTimers.Values)
            {
                timer?.Dispose();
            }
            _serverQueryTimers.Clear();
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
                async _ => await PollServerStatus(profile).ConfigureAwait(false),
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
                    
                    // Find and notify the specific profile view model on UI thread
                    _syncContext?.Post(_ =>
                    {
                        var profileViewModel = ServerProfiles.FirstOrDefault(p => p.ServerProfile.Id == profile.Id);
                        profileViewModel?.NotifyAllPropertiesChanged();
                    }, null);
                }

                // Execute query on background thread
                var status = await Task.Run(async () => 
                    await _queryService.QueryServerFullAsync(profile.Uri, profile.MinecraftPort)
                ).ConfigureAwait(false);
                
                SetServerStatus(profile.Id, status);
                
                // Mark as completed first poll and set loading to false only if this was the first poll
                if (!_hasCompletedFirstPoll.ContainsKey(profile.Id) || !_hasCompletedFirstPoll[profile.Id])
                {
                    _hasCompletedFirstPoll[profile.Id] = true;
                    SetLoadingState(profile.Id, false);
                }
                
                // Notify UI thread that this specific profile's status has updated
                _syncContext?.Post(_ =>
                {
                    var profileViewModelAfter = ServerProfiles.FirstOrDefault(p => p.ServerProfile.Id == profile.Id);
                    profileViewModelAfter?.NotifyAllPropertiesChanged();
                }, null);
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
                
                // Find and notify the specific profile view model on UI thread
                _syncContext?.Post(_ =>
                {
                    var profileViewModel = ServerProfiles.FirstOrDefault(p => p.ServerProfile.Id == profile.Id);
                    profileViewModel?.NotifyAllPropertiesChanged();
                }, null);
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
                
                // Sort profiles to ensure the new profile is in the correct position
                SortServerProfiles(force: true);
                
                // Start polling for the new server profile only if polling is active
                if (_serverQueryTimers.Count > 0 || ServerProfiles.Count == 1)
                {
                    StartPollingForProfile(newProfile);
                }
                
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

                // Check if polling was active before stopping it
                var wasPollingActive = _serverQueryTimers.Count > 0;

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
                    
                    // Refresh the collection 
                    await LoadServerProfilesAsync();
                    
                    // Restart polling only if it was active before
                    if (wasPollingActive)
                    {
                        StartServerPolling();
                    }
                    
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
            
            // Stop all polling
            StopAllPolling();
            
            // Clear all tracking dictionaries
            _serverStatuses.Clear();
            _loadingStates.Clear();
            _hasCompletedFirstPoll.Clear();
            
            _cancellationTokenSource?.Dispose();
        }
    }
}
