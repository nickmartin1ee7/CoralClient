using CommunityToolkit.Mvvm.ComponentModel;

namespace CoralClientMobileApp.Model
{
    public partial class Player : ObservableObject
    {
        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _status = "Online";

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