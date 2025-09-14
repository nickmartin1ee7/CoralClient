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

        public void NotifyAllPropertiesChanged()
        {
            OnPropertyChanged(nameof(IsLoading));
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(IsOnline));
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(VersionText));
            OnPropertyChanged(nameof(PingText));
        }
    }
}