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
            get { return BackgroundManager.Instance.IsWorking; }
        }


        public ICommand BuyMeACoffee
        {
            get;
        }


        public AppShell()
        {
            InitializeComponent();
            this.BindingContext = this;

            /*Routing.RegisterRoute("//Destinations", typeof(ParkChoice));
            Routing.RegisterRoute("//Attractions", typeof(ListAttractions));
            Routing.RegisterRoute("//Map", typeof(Map));*/

            BuyMeACoffee = new Command(async () =>
            {
                await Browser.Default.OpenAsync("https://buymeacoffee.com/taywon", BrowserLaunchMode.SystemPreferred);
            });
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            NotificationService.Instance.Init();

            if (HaveDestination)
            {
                await GoToAsync("//Attractions");
                
            }
                
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

        private async void OnStartBackgroundListening(object sender, EventArgs e)
        {
            BackgroundManager.Instance.StartWorkingBackground();
            OnPropertyChanged(nameof(IsBackgroundWatching));
        }

        private async void OnStopBackgroundListening(object sender, EventArgs e)
        {
            BackgroundManager.Instance.StopWorkingBackground();
            OnPropertyChanged(nameof(IsBackgroundWatching));
        }

    }
}