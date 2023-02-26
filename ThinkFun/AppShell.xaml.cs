using ThinkFun.Views;

namespace ThinkFun
{
    public partial class AppShell : Shell
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
        }

        public void FlushDestination()
        {
            OnPropertyChanged(nameof(HaveDestination));
        }
    }
}