using CommunityToolkit.Mvvm.ComponentModel;

namespace CoralClientMobileApp.Model
{
    public partial class Player : ObservableObject
    {
        [ObservableProperty]
        private string _name = string.Empty;

        public Player()
        {
        }

        public Player(string name)
        {
            Name = name;
        }
    }
}