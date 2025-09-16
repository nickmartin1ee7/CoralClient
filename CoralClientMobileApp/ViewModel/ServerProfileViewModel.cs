using CoralClientMobileApp.Helpers;
using CoralClientMobileApp.Model;

namespace CoralClientMobileApp.ViewModel
{
    public partial class ServerProfileViewModel : BaseObservableViewModel
    {
        private readonly MainPageViewModel _mainPageViewModel;
        
        public ServerProfile ServerProfile { get; }

        public ServerProfileViewModel(ServerProfile serverProfile, MainPageViewModel mainPageViewModel)
        {
            ServerProfile = serverProfile;
            _mainPageViewModel = mainPageViewModel;
        }

        public string ServerUriText => ServerProfile.ServerUriText;
        
        public bool IsLoading => _mainPageViewModel.IsProfileLoading(ServerProfile.Id);
        
        public ServerStatus? Status => _mainPageViewModel.GetServerStatus(ServerProfile.Id);
        
        public bool IsOnline => Status?.IsOnline ?? false;
        
        public string StatusText
        {
            get
            {
                var status = Status;
                if (status?.IsOnline == true)
                    return $"Online ({status.OnlinePlayers}/{status.MaxPlayers} players)";
                return status?.ErrorMessage ?? "Offline";
            }
        }
        
        public string VersionText => Status?.VersionName?.RemoveColorCodes()?.Trim() ?? "";
        
        public string PingText => Status?.IsOnline == true ? $"{Status.Ping}ms" : "";
        
        public string MotdText => Status?.StrippedMotd?.RemoveColorCodes()?.Trim() ?? "";
        
        public string GameTypeText => Status?.GameType ?? "";
        
        public byte[]? FaviconBytes => Status?.FaviconBytes;
        
        public bool HasFavicon => Status?.FaviconBytes?.Length > 0;
        
        public string HostText => Status?.IsOnline == true ? $"{Status.HostIp}:{Status.HostPort}" : "";
        
        public string DetailedServerInfo
        {
            get
            {
                var status = Status;
                if (status?.IsOnline != true) return "";
                
                var parts = new List<string>();
                
                if (!string.IsNullOrEmpty(status.GameType))
                    parts.Add($"Game: {status.GameType?.RemoveColorCodes()?.Trim()}");
                    
                if (!string.IsNullOrEmpty(status.HostIp))
                    parts.Add($"Host: {status.HostIp}:{status.HostPort}");
                
                return string.Join(Environment.NewLine, parts).Trim();
            }
        }
        
        public bool HasPlayerList => Status?.PlayerList?.Any() == true;
        
        public string PlayerListText
        {
            get
            {
                var status = Status;
                if (status?.PlayerList?.Any() != true)
                    return "";
                    
                return $"Players: {string.Join(", ", status.PlayerList
                    .Select(player => player?.RemoveColorCodes()?.Trim())
                    .OrderBy(p => p))}";
            }
        }

        public void NotifyAllPropertiesChanged()
        {
            OnPropertyChanged(nameof(IsLoading));
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(IsOnline));
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(VersionText));
            OnPropertyChanged(nameof(PingText));
            OnPropertyChanged(nameof(MotdText));
            OnPropertyChanged(nameof(GameTypeText));
            OnPropertyChanged(nameof(FaviconBytes));
            OnPropertyChanged(nameof(HasFavicon));
            OnPropertyChanged(nameof(HostText));
            OnPropertyChanged(nameof(DetailedServerInfo));
            OnPropertyChanged(nameof(HasPlayerList));
            OnPropertyChanged(nameof(PlayerListText));
        }
    }
}