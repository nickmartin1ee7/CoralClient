using CoralClientMobileApp.Model;
using CoralClientMobileApp.ViewModel;
using MinecraftRcon;

namespace CoralClientMobileApp.View
{
    public partial class RconPage : ContentPage
    {
        private readonly RconPageViewModel _vm;

        public RconPage(ServerProfile serverProfile)
        {
            InitializeComponent();

            // Create RconClient for this specific page instance
            var rconClient = new RconClient();
            _vm = new RconPageViewModel(serverProfile, rconClient);

            BindingContext = _vm;
        }

        protected override bool OnBackButtonPressed()
        {
            _vm.Dispose();
            return base.OnBackButtonPressed();
        }
    }
}
