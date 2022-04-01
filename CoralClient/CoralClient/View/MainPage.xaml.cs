using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoralClient.ViewModel;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace CoralClient.View
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MainPage : ContentPage
    {
        private readonly MainPageViewModel _vm;

        public MainPage()
        {
            InitializeComponent();
            _vm = new MainPageViewModel((title, message) =>
                DisplayPromptAsync(title, message));
        }
    }
}