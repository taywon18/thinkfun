using ThinkFun.Views;
using Map = ThinkFun.Views.Map;

namespace ThinkFun
{
    public partial class AppShell 
        : Shell
    {
        public bool HaveDestination
        {
            get { return DataManager.Instance.HasDestination; }
        }

        public AppShell()
        {
            InitializeComponent();
            this.BindingContext = this;

            Routing.RegisterRoute("//Destinations", typeof(ParkChoice));
            Routing.RegisterRoute("//Attractions", typeof(ListAttractions));
            Routing.RegisterRoute("//Map", typeof(Map));
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (HaveDestination)
            {
                await GoToAsync("//Attractions");
            }
                
        }

        public void FlushDestination()
        {
            OnPropertyChanged(nameof(HaveDestination));
        }
    }
}