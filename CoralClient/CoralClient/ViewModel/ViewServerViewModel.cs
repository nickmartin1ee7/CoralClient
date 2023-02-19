using System.Windows.Input;

using CoralClient.Model;

using Xamarin.Forms;

namespace CoralClient.ViewModel
{
    public class ViewServerViewModel : BaseViewModel
    {
        public ServerProfile ServerProfile { get; }

        public ViewServerViewModel(ServerProfile serverProfile)
        {
            ServerProfile = serverProfile;
        }

        public ICommand BackCommand => new Command(() => Application.Current.MainPage.Navigation.PopAsync());
    }
}
