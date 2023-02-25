using System.Collections.ObjectModel;

namespace ThinkFun.Views;

public partial class ListAttractions : ContentPage
{
	public class Attraction
	{
		public string Name { get; set; } = "";
		public string WaitingTime { get; set; } = "";
		public string Distance { get; set; } = "";
	}

    ObservableCollection<Attraction> Attractions = new ObservableCollection<Attraction>();

    public ListAttractions()
	{
		InitializeComponent();

        Attractions.Add(new Attraction
        {
            Name = "It's a smallworld",
            Distance = "Disneyland Paris (200m)",
            WaitingTime = "65min"
        });
        Attractions.Add(new Attraction
        {
            Name = "Hyperspace Mountain",
            Distance = "Disneyland Paris (200m)",
            WaitingTime = "25min"
        });



        AttractionList.ItemsSource = Attractions;
    }
}