using System;
using System.ComponentModel;
using System.Windows.Input;
using ThinkFun.Views;
using Map = ThinkFun.Views.Map;

namespace ThinkFun
{
    public partial class AppShell 
        : Shell
        , INotifyPropertyChanged
    {
        public bool HaveDestination
        {
            get { return DataManager.Instance.HasDestination; }
        }

        public bool IsListeningLocation
        {
            get { return LocationManager.Instance.IsListening; }
        }

        public bool IsBackgroundWatching
        {
            get { return NotificationService.Instance.IsWorking; }
        }

        public bool IsConnected
        {
            get { return LoginManager.Instance.LastUser != null; }
        }


        public AppShell()
        {
            InitializeComponent();
            this.BindingContext = this;

            /*Routing.RegisterRoute("//Destinations", typeof(ParkChoice));
            Routing.RegisterRoute("//Attractions", typeof(ListAttractions));
            Routing.RegisterRoute("//Map", typeof(Map));*/
            Routing.RegisterRoute("//Login", typeof(LoginPage));
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            FlushConnectionState();

            NotificationService.Instance.StopWorkingBackground();
            OnPropertyChanged(nameof(IsBackgroundWatching));


            if (HaveDestination)
            {
                await GoToAsync("//Attractions");
            }
        }

        public async void FlushConnectionState(bool actualize = true)
        {
            if(actualize)
                await LoginManager.Instance.CheckIsConnected();
            OnPropertyChanged(nameof(IsConnected));
        }

        public void FlushDestination()
        {
            OnPropertyChanged(nameof(HaveDestination));
        }

        private async void OnStartLocationListening(object sender, EventArgs e)
        {
            await LocationManager.Instance.StartListening();
            OnPropertyChanged(nameof(IsListeningLocation));
        }

        private async void OnStopLocationListening(object sender, EventArgs e)
        {
            await LocationManager.Instance.StopListening();
            OnPropertyChanged(nameof(IsListeningLocation));
        }

        private void OnStartBackgroundListening(object sender, EventArgs e)
        {
            NotificationService.Instance.StartWorkingBackground();
            OnPropertyChanged(nameof(IsBackgroundWatching));
        }

        private void OnStopBackgroundListening(object sender, EventArgs e)
        {
            NotificationService.Instance.StopWorkingBackground();
            OnPropertyChanged(nameof(IsBackgroundWatching));
        }

        private async void Login_Tapped(object sender, TappedEventArgs e)
        {
            await Navigation.PushAsync(new LoginPage());
        }

        private async void Connected_Tapped(object sender, TappedEventArgs e)
        {
            bool co = await LoginManager.Instance.CheckIsConnected();
        }

        private async void BuyMeACoffre_Tapped(object sender, TappedEventArgs e)
        {
            await Browser.Default.OpenAsync("https://buymeacoffee.com/taywon", BrowserLaunchMode.SystemPreferred);
        }
    }
}