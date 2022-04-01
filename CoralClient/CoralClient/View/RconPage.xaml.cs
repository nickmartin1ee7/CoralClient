using CoralClient.Model;
using CoralClient.ViewModel;
using Xamarin.Forms;

namespace CoralClient.View
{
    public partial class RconPage : ContentPage
    {
        public RconPage(ServerProfile serverProfile)
        {
            InitializeComponent();

            var vm = new RconPageViewModel(serverProfile);

            BindingContext = vm;
        }
    }
}
