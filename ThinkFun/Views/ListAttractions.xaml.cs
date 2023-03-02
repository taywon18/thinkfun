using System;
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
    public Microsoft.Maui.Dispatching.IDispatcherTimer UpdateTimer { get; }
    bool InitialFetchDone = false;

    object RefresherMutex = new();

    public class AttractionView
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
    FastObservableCollection<AttractionView> Attractions = new FastObservableCollection<AttractionView>();

    public ListAttractions()
    {
        InitializeComponent();

        this.BindingContext = this;
        AttractionList.ItemsSource = Attractions;
        AttractionList.RefreshCommand = new Command(() => OnListRefresh());

        UpdateTimer = Application.Current.Dispatcher.CreateTimer();
        UpdateTimer.Interval = TimeSpan.FromSeconds(30);
        UpdateTimer.Tick += async (s, e) => await RefreshList();
        UpdateTimer.IsRepeating = true;
        UpdateTimer.Start();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await RefreshList(true);
    }

    public async void OnListRefresh()
    {
        await RefreshList(true);
    }


    public async Task RefreshList(bool forceRefresh = false)
    {
        if (InitialFetchDone && AttractionList.IsRefreshing)
            return;
        InitialFetchDone = true;

        AttractionList.IsRefreshing = true;

        if (forceRefresh)
            await DataManager.Instance.Update();

        var staticdata = await DataManager.Instance.GetStaticDataBuffered();
        var rawlivedata = await DataManager.Instance.GetLiveDataBuffered();
        var livedata = new Dictionary<string, Model.LiveData>(rawlivedata);
        var currentPosition = await LocationManager.Instance.GetPositionAsync();

        var newView = new List<AttractionView>();
        foreach (var parkelement in staticdata)
        {
            if (parkelement is not Model.Attraction)
                continue;
            var attraction = parkelement as Model.Attraction;

            AttractionView attraction_viewmodel = new()
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

            newView.Add(attraction_viewmodel);
        }

        var newViewFiltered = Filter(newView);
        var newViewFilteredAndOrdered = Sort(newViewFiltered);

        UpdateObservable(Attractions, newViewFilteredAndOrdered);

        AttractionList.IsRefreshing = false;
    }

    protected void UpdateObservable(FastObservableCollection<AttractionView> view, IEnumerable<AttractionView> list)
    {
        /*HashSet<ParkElement> newlistAsHash = new(list.Select(x => x.ParkElement));
        Dictionary<string, int> oldlistAsHash = new();

        for(int i = 0; i < view.Count; i++)
        {
            var e = view[i];
            
            //remove nonexisting elements
            if(!newlistAsHash.Contains(e))
            {
                view.RemoveAt(i);
                i--;
                return;
            }

            //modify existing element
            view.Add(i);
        }*/

        view.SuspendCollectionChangeNotification();
        view.Clear();
        foreach (var i in list)
            view.Add(i);
        view.ResumeCollectionChangeNotification();
        view.NotifyChanges();
    }

    protected IEnumerable<AttractionView> Sort(IEnumerable<AttractionView> attraction)
    {
        if (SortBy == SortMode.ByDistance)
            attraction = attraction.OrderBy(x => x.DistanceDbl);

        if (InverseSort)
            attraction = attraction.Reverse();

        if (FilterType == 1)
            attraction = attraction.Where(x => x.ParkElement is Model.Attraction);
        else if (FilterType == 2)
            attraction = attraction.Where(x => x.ParkElement is Show);
        else if (FilterType == 3)
            attraction = attraction.Where(x => x.ParkElement is Restaurant);

        return attraction;
    }

    protected IEnumerable<AttractionView> Filter(IEnumerable<AttractionView> attraction)
    {
        if (FilterStatus == 1)
            attraction = attraction.Where(x =>
            {
                if (x.LiveData is Model.Queue queue)
                {
                    return queue.Status == Status.OPENED || queue.Status == Status.DOWN;
                }

                return true;
            });
        else if (FilterStatus == 2)
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

        return attraction;
    }

    private async void TypePicker_SelectedIndexChanged(object sender, EventArgs e)
    {
        await RefreshList();
    }
}