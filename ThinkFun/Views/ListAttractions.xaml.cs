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
    
    public int FilterType { 
        get => DataManager.Instance.Configuration.FilterType; 
        set
        {
            DataManager.Instance.Configuration.FilterType = value;
            DataManager.Instance.SaveConfig();
        }
    }    
    public int FilterStatus { 
        get => DataManager.Instance.Configuration.FilterStatus; 
        set
        {
            DataManager.Instance.Configuration.FilterStatus = value;
            DataManager.Instance.SaveConfig();
        }
    }
    public string ParkFilter = null;
    public bool FilterClosed { get; set; } = true;

    object RefresherMutex = new();

    public class Attraction
    {
        public Model.ParkElement ParkElement { get; set; }
        public Model.LiveData LiveData { get; set; }
        public double? DistanceDbl { get; set; }

        public string NameDisplayable
        {
            get => ParkElement.Name ?? "�l�ment";
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
                        return "Cass� :'(";
                    else if (queue.Status == Model.Status.CLOSED)
                        return "Ferm�";
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

        this.BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await RefreshList(true);
    }

    public async Task RefreshList(bool forceRefresh = false)
    {
        await MainThread.InvokeOnMainThreadAsync(
        async () =>
        {
            AttractionList.IsRefreshing = true;

            if (forceRefresh)
                await DataManager.Instance.Update();

            var livedata = new Dictionary<string, Model.LiveData>(DataManager.Instance.BufferedLiveDatas);
            var currentPosition = await LocationManager.Instance.GetPositionAsync();

            Attractions.Clear();

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

                    var distM = Location.CalculateDistance(here, destpos, DistanceUnits.Kilometers) * 1000;
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
        });
    }

    protected IEnumerable<Attraction> SortAndFilter(IEnumerable<Attraction> attraction)
    {
        //Sort

        if(SortBy == SortMode.ByDistance)
            attraction = attraction.OrderBy(x => x.DistanceDbl);

        if (InverseSort)
            attraction = attraction.Reverse();

        if (FilterType == 1)
            attraction = attraction.Where(x => x.ParkElement is Model.Attraction);
        else if (FilterType == 2)
            attraction = attraction.Where(x => x.ParkElement is Show);        
        else if (FilterType == 3)
            attraction = attraction.Where(x => x.ParkElement is Restaurant);

        //Filter

        if(FilterStatus == 1)
            attraction = attraction.Where(x =>
            {
                if(x.LiveData is Model.Queue queue)
                {
                    return queue.Status == Status.OPENED || queue.Status == Status.DOWN;
                }

                return true;    
            });
        else if(FilterStatus == 2)
            attraction = attraction.Where(x =>
            {
                if (x.LiveData is Model.Queue queue)
                {
                    return queue.Status == Status.OPENED;
                }

                return true;
            });
        else if (FilterStatus == 3)
            attraction = attraction.Where(x =>
            {
                if (x.LiveData is Model.Queue queue)
                {
                    return queue.Status == Status.CLOSED || queue.Status == Status.DOWN;
                }

                return true;
            });
        /*if (FilterClosed)
            attraction = attraction.Where(x => (x.Status == Status.OPENED || x.Status == Status.DOWN));*/

        return attraction;
    }

    private async void TypePicker_SelectedIndexChanged(object sender, EventArgs e)
    {
        await RefreshList();
    }
}