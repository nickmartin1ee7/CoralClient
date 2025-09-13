using CoralClientMobileApp.ViewModel;

namespace CoralClientMobileApp.View;

public partial class ServerProfilePage : ContentPage
{
    public ServerProfilePage(ServerProfilePageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}