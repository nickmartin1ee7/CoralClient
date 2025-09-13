namespace CoralClientMobileApp;

public partial class AppShell : Shell
{
	public AppShell(MainPage mainPage)
	{
		InitializeComponent();
		
		// Register the MainPage with Shell routing
		Routing.RegisterRoute("MainPage", typeof(MainPage));
	}
}
