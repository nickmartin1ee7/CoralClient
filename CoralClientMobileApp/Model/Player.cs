using CommunityToolkit.Mvvm.ComponentModel;

namespace CoralClientMobileApp.Model
{
    public partial class Player : ObservableObject
    {
        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string? _uuid;

        [ObservableProperty]
        private string? _avatarUrl;

        [ObservableProperty]
        private bool _isLoadingAvatar;

        public Player()
        {
        }

        public Player(string name)
        {
            Name = name;
        }
    }
}