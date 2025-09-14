using CommunityToolkit.Mvvm.ComponentModel;

namespace CoralClientMobileApp.Model
{
    public partial class Player : ObservableObject
    {
        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _status = "Online";

        [ObservableProperty]
        private string _gameMode = "Survival";

        [ObservableProperty]
        private bool _isOperator;

        [ObservableProperty]
        private string _dimension = "Overworld";

        public Player()
        {
        }

        public Player(string name, string status = "Online")
        {
            Name = name;
            Status = status;
        }
    }
}