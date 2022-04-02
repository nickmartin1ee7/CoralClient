using CoralClient.Model;
using CoralClient.Services;
using CoralClient.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using Xamarin.Forms;

namespace CoralClient.View
{
    public partial class RconPage : ContentPage
    {
        public RconPage(ServerProfile serverProfile)
        {
            InitializeComponent();

            var vm = new RconPageViewModel(serverProfile,
                Dependencies.ServiceProvider.GetService<RconService>());

            BindingContext = vm;
        }
    }
}
