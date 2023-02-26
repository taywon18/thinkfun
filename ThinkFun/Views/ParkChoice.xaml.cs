using System.Collections.ObjectModel;

namespace ThinkFun.Views;

public partial class ParkChoice : ContentPage
{
	public class Destination
	{
        public string Id { get; set; }
        public string Name { get; set; } = "";
        public string Distance { get; set; } = "";
	}

	ObservableCollection<Destination> Destinations = new ObservableCollection<Destination>();

    public ParkChoice()
	{
		InitializeComponent();

		ParkList.ItemsSource = Destinations;
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        ParkList.IsRefreshing = true;

		var destinations = await DataManager.Instance.Data.GetDestinations();
        var currentPosition = await LocationManager.Instance.GetPositionAsync();

		List<Destination> destinationlist = new();
        foreach (var destination in destinations)
        {
            Destination destination_viewmodel = new()
            {
                Id = destination.UniqueIdentifier,
                Name = destination.Name
            };

            if (currentPosition != null && currentPosition.HasLatitudeLongitude)
            {
                Location here = new Location(currentPosition.Latitude, currentPosition.Longitude);
                Location destpos = new Location(destination.Position.Latitude, destination.Position.Longitude);

                var distKm = Location.CalculateDistance(here, destpos, DistanceUnits.Kilometers);
                destination_viewmodel.Distance = String.Format("{0:0}km", distKm);
            }

            destinationlist.Add(destination_viewmodel);
        }

        Destinations.Clear();
        foreach (var i in destinationlist)
            Destinations.Add(i);
        ParkList.IsRefreshing = false;
    }

    private async void ParkList_ItemSelected(object sender, SelectedItemChangedEventArgs e)
    {
        var item = e.SelectedItem as Destination;
        DataManager.Instance.SetCurrentDestination(item.Id);
        ((AppShell)Shell.Current).FlushDestination();

        await Shell.Current.GoToAsync("//Attractions");
    }
}