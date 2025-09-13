using CoralClientMobileApp.Model;
using CoralClientMobileApp.ViewModel;

namespace CoralClientMobileApp.View
{
    public partial class RconPage : ContentPage
    {
        private RconPageViewModel? _vm;
        private readonly Func<ServerProfile, RconPageViewModel> _rconViewModelFactory;

        public RconPage(Func<ServerProfile, RconPageViewModel> rconViewModelFactory)
        {
            InitializeComponent();
            _rconViewModelFactory = rconViewModelFactory;
        }

        public void Initialize(ServerProfile serverProfile)
        {
            _vm = _rconViewModelFactory(serverProfile);
            BindingContext = _vm;
        }

        protected override bool OnBackButtonPressed()
        {
            _vm?.Dispose();
            return base.OnBackButtonPressed();
        }
    }
}
