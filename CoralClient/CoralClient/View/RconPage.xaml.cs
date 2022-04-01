using CoralClient.Model;
using CoralClient.ViewModel;
using Xamarin.Forms;

namespace CoralClient.View
{
    public partial class RconPage : ContentPage
    {
        private readonly ServerProfile _serverProfile;
        private readonly RconPageViewModel _vm = new RconPageViewModel();

        public RconPage(ServerProfile serverProfile)
        {
            _serverProfile = serverProfile;

            InitializeComponent();

            BindingContext = _vm;
        }
    }
}
