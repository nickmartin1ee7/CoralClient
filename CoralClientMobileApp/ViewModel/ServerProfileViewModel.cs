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
        
        public string VersionText => Status?.VersionName ?? "";
        
        public string PingText => Status?.IsOnline == true ? $"{Status.Ping}ms" : "";
        
        // New properties for McQuery.Net information
        public string MotdText => Status?.Motd ?? "";
        
        public string GameTypeText => Status?.GameType ?? "";
        
        public string MapText => Status?.Map ?? "";
        
        public string DetailedServerInfo
        {
            get
            {
                var status = Status;
                if (status?.IsOnline != true) return "";
                
                var parts = new List<string>();
                
                if (!string.IsNullOrEmpty(status.GameType))
                    parts.Add($"Game: {status.GameType}");
                    
                if (!string.IsNullOrEmpty(status.Map))
                    parts.Add($"Map: {status.Map}");
                    
                if (!string.IsNullOrEmpty(status.GameId))
                    parts.Add($"Game ID: {status.GameId}");
                
                return string.Join(" • ", parts);
            }
        }
        
        public string PluginsText
        {
            get
            {
                var status = Status;
                if (string.IsNullOrEmpty(status?.Plugins))
                    return "";
                    
                return $"Plugins: {status.Plugins}";
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
                    
                return $"Players: {string.Join(", ", status.PlayerList)}";
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
            OnPropertyChanged(nameof(MapText));
            OnPropertyChanged(nameof(DetailedServerInfo));
            OnPropertyChanged(nameof(PluginsText));
            OnPropertyChanged(nameof(HasPlayerList));
            OnPropertyChanged(nameof(PlayerListText));
        }
    }
}