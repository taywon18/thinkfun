using System.Collections.ObjectModel;

namespace ThinkFun.Views;

public partial class ParkChoice : ContentPage
{
	public class Destination
	{
		public string Name { get; set; } = "";
        public string Distance { get; set; } = "";
	}

	ObservableCollection<Destination> Destinations = new ObservableCollection<Destination>();

    public ParkChoice()
	{
		InitializeComponent();

		Destinations.Add(new Destination
		{
			Name = "Disney",
			Distance = "200km"
		});		
		
		Destinations.Add(new Destination
		{
			Name = "Asterix",
			Distance = "230km"
		});

		ParkList.ItemsSource = Destinations;
	}

    protected override void OnAppearing()
    {
        base.OnAppearing();
		Task.Run(async () =>
		{
            await DataManager.Instance.Data.FlushDestinations();
        }).Wait();
    }
}