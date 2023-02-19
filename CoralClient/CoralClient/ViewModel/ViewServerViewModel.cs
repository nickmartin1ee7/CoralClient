using System.Windows.Input;

using CoralClient.Model;

using Xamarin.Forms;

namespace CoralClient.ViewModel
{
    public class ViewServerViewModel : BaseViewModel
    {
        private bool _isEditing;

        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                if (value == _isEditing) return;
                _isEditing = value;
                OnPropertyChanged();
            }
        }

        public ServerProfile ServerProfile { get; }

        public ViewServerViewModel(ServerProfile serverProfile)
        {
            ServerProfile = serverProfile;
        }

        public ICommand BackCommand => new Command(() =>
            Application.Current.MainPage.Navigation.PopAsync());

        public ICommand EditUriCommand => new Command(() =>
            Application.Current.MainPage.Navigation.PopAsync());

        public ICommand EditMinecraftPortCommand => new Command(() =>
            Application.Current.MainPage.Navigation.PopAsync());

        public ICommand EditRconPortCommand => new Command(() =>
            Application.Current.MainPage.Navigation.PopAsync());

        public ICommand EditPasswordCommand => new Command(() =>
            Application.Current.MainPage.Navigation.PopAsync());

        public ICommand EditImageCommand => new Command(() =>
            Application.Current.MainPage.Navigation.PopAsync());
    }
}
