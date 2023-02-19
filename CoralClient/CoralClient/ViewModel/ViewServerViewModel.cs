using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

using CoralClient.DbContext;
using CoralClient.Model;
using CoralClient.Services;

using Microsoft.Extensions.DependencyInjection;

using Xamarin.Forms;

namespace CoralClient.ViewModel
{
    public class ViewServerViewModel : BaseViewModel
    {
        public ViewServerViewModel()
        {
            ServerProfiles = GetServerProfiles();
        }

        public List<ServerProfile> ServerProfiles { get; set; }

        public ICommand BackCommand => new Command(() => Application.Current.MainPage.Navigation.PopAsync());

        private List<ServerProfile> GetServerProfiles() =>
            Dependencies.ServiceProvider
                .GetRequiredService<ServerProfileContext>().ServerProfiles
                .ToList();
    }
}
