using System;
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
    public class MainViewModel : BaseViewModel
    {
        public MainViewModel()
        {
            ServerProfiles = GetServerProfiles();
        }

        public List<ServerProfile> ServerProfiles { get; set; }

        public ICommand OrderCommand => new Command(() =>
            Application.Current.MainPage.Navigation.PushAsync(new OrderPage()));

        private List<ServerProfile> GetServerProfiles() =>
            Dependencies.ServiceProvider
                .GetService<ServerProfileContext>().ServerProfiles
                .ToList();
    }
}
