using System.Collections.ObjectModel;
using ThinkFun.Model;

namespace ThinkFun.Views;

public partial class ListAttractions : ContentPage
{
    public enum SortMode
    {
        ByDistance,
        ByWaitingTime,
        BySingleWaitingTime
    }
    public SortMode SortBy = default;
    public bool InverseSort = false;
    
    public enum FilterTypesMode
    {
        Everything,
        Attraction,
        Restaurant,
        Show
    }
    public SortMode FilterType = default;
    public string ParkFilter = null;
    public bool FilterClosed { get; set; } = true;

    public class Attraction
    {
        public Model.ParkElement ParkElement { get; set; }
        public Model.LiveData LiveData { get; set; }
        public double? DistanceDbl { get; set; }

        public string NameDisplayable
        {
            get => ParkElement.Name ?? "Élément";
        }

        public string DescriptionDisplayable
        {
            get => DistanceDbl.HasValue ? String.Format("{0:0}m", DistanceDbl.Value) : "?";
        }

        public string StatusDisplayable
        {
            get
            {
                if (ParkElement is Model.Attraction)
                {
                    if (LiveData is not Model.Queue)
                        return "?";

                    var queue = (LiveData as Model.Queue);

                    if (queue.Status == Model.Status.OPENED)
                    {
                        if (queue.SingleRiderWaitTime.HasValue && queue.ClassicWaitTime.HasValue)
                            return String.Format("{0}min ({1}min)", queue.ClassicWaitTime.Value.TotalMinutes, queue.SingleRiderWaitTime.Value.TotalMinutes);
                        else if (queue.ClassicWaitTime.HasValue)
                            return String.Format("{0}min", queue.ClassicWaitTime.Value.TotalMinutes);
                        else
                            return "Ouvert";
                    }
                    else if (queue.Status == Model.Status.DOWN)
                        return "Cassé :'(";
                    else if (queue.Status == Model.Status.CLOSED)
                        return "Fermé";
                    else
                        return queue.Status.ToString();
                }

                return "";
            }
        }
    }

    ObservableCollection<Attraction> Attractions = new ObservableCollection<Attraction>();

    public ListAttractions()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        AttractionList.IsRefreshing = true;

        await DataManager.Instance.Update();
        Attractions.Clear();

        var livedata = new Dictionary<string, Model.LiveData>(DataManager.Instance.BufferedLiveDatas);
        var currentPosition = await LocationManager.Instance.GetPositionAsync();

        foreach (var parkelement in DataManager.Instance.BufferedElements)
        {
            if (parkelement is not Model.Attraction)
                continue;
            var attraction = parkelement as Model.Attraction;


            Attraction attraction_viewmodel = new()
            {
                ParkElement = attraction
            };

            if (currentPosition != null && currentPosition.HasLatitudeLongitude)
            {
                Location here = new Location(currentPosition.Latitude, currentPosition.Longitude);
                Location destpos = new Location(attraction.Position.Latitude, attraction.Position.Longitude);

                var distM = Location.CalculateDistance(here, destpos, DistanceUnits.Kilometers)*1000;
                attraction_viewmodel.DistanceDbl = distM;
            }

            if (livedata.ContainsKey(parkelement.UniqueIdentifier))
            {
                var ld = livedata[parkelement.UniqueIdentifier];

                attraction_viewmodel.LiveData = ld;
            }
            
            Attractions.Add(attraction_viewmodel);
        }

        Attractions = new ObservableCollection<Attraction>(SortAndFilter(Attractions));

        AttractionList.ItemsSource = Attractions;

        AttractionList.IsRefreshing = false;
    }

    protected IEnumerable<Attraction> SortAndFilter(IEnumerable<Attraction> attraction)
    {
        
        //Sort

        if(SortBy == SortMode.ByDistance)
            attraction = attraction.OrderBy(x => x.DistanceDbl);

        if (InverseSort)
            attraction = attraction.Reverse();

        //Filter
        /*if (FilterClosed)
            attraction = attraction.Where(x => (x.Status == Status.OPENED || x.Status == Status.DOWN));*/

        return attraction;
    }
}