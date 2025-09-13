using CoralClient.Model;
using CoralClient.Services;
using CoralClient.ViewModel;

namespace CoralClient.View
{
    public partial class RconPage : ContentPage
    {
        private readonly RconPageViewModel _vm;

        public RconPage(ServerProfile serverProfile)
        {
            InitializeComponent();

            _vm = new RconPageViewModel(serverProfile,
                Dependencies.ServiceProvider.GetService<RconClient>());

            BindingContext = _vm;
        }

        protected override bool OnBackButtonPressed()
        {
            _vm.Dispose();
            return base.OnBackButtonPressed();
        }
    }
}
