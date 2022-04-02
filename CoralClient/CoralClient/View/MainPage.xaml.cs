using CoralClient.DbContext;
using CoralClient.Services;
using CoralClient.ViewModel;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace CoralClient.View
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();

            var vm = new MainPageViewModel(Dependencies.ServiceProvider.GetService<ServerProfileContext>(),
                (title, message) => DisplayPromptAsync(title, message),
                (serverProfile) => Navigation.PushModalAsync(new RconPage(serverProfile)));

            BindingContext = vm;
        }
    }
}