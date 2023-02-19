using CoralClient.Model;
using CoralClient.ViewModel;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace CoralClient
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class ViewServerPage : ContentPage
	{
		public ViewServerPage(ServerProfile serverProfile)
		{
			InitializeComponent();
			BindingContext = new ViewServerViewModel(serverProfile);
		}
	}
}