using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using ThinkFun.Model;
using CommunityToolkit.Maui.Alerts;


namespace ThinkFun.Views;


public partial class ListAttractions 
    : ContentPage
{
    public class ParkItemView
       : INotifyPropertyChanged
    {

        ListAttractions Main { get; }
        public ParkItemView(ListAttractions attr)
        {
            Main = attr;
            ToggleFavorite = new Command(() =>
            {
                Favorite = !Favorite;
                OnPropertyChanged(nameof(Favorite));
                OnPropertyChanged(nameof(NotFavorite));
            });
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public Model.ParkElement ParkElement { get; set; }
        public Model.LiveData LiveData { get; set; }
        public double? DistanceDbl { get; set; }

        public bool Favorite
        {
            get
            {
                return DataManager.Instance.Configuration.FavoriteElements.Contains(ParkElement.UniqueIdentifier);
            }
            set
            {
                if (value)
                    DataManager.Instance.Configuration.FavoriteElements.Add(ParkElement.UniqueIdentifier);
                else
                    DataManager.Instance.Configuration.FavoriteElements.Remove(ParkElement.UniqueIdentifier);
                DataManager.Instance.SaveConfig();
                Main.Resort();
            }
        }
        public bool NotFavorite { get => !Favorite; set => Favorite = !value; }
        public ICommand ToggleFavorite
        {
            get;
        }

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
                        return "🛠️";
                    else if (queue.Status == Model.Status.CLOSED)
                        return "🚫";
                    else
                        return queue.Status.ToString();
                }

                return "";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

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
    public int FilterStatus 
    { 
        get => DataManager.Instance.Configuration.FilterStatus; 
        set
        {
            DataManager.Instance.Configuration.FilterStatus = value;
            DataManager.Instance.SaveConfig();
        }
    }
    public int FilterPark
    {
        get => DataManager.Instance.Configuration.FilterPark;
        set
        {
            if (FreezeParkList || value == -1)
                return;

            DataManager.Instance.Configuration.FilterPark = value;
            DataManager.Instance.SaveConfig();
        }
    }

    public bool CompactDisplay
    {
        get => DataManager.Instance.Configuration.CompactDisplay;
        set
        {
            DataManager.Instance.Configuration.CompactDisplay = value;
            DataManager.Instance.SaveConfig();
            OnPropertyChanged(nameof(CompactDisplay));
            OnPropertyChanged(nameof(VerboseDisplay));
        }
    }

    public bool VerboseDisplay
    {
        get => !CompactDisplay;
        set => CompactDisplay = !value;
    }

    public string ParkFilter = null;
    public bool FilterClosed { get; set; } = true;
    public Microsoft.Maui.Dispatching.IDispatcherTimer UpdateTimer { get; }
    bool InitialFetchDone = false;

    public FastObservableCollection<string> Parks = new ();
    bool FreezeParkList = false;

    SemaphoreSlim RefresherMutex = new(1);
    public bool IsRefreshing
    {
        get;
        set;
    }
   

    public async void Resort()
    {
        await RefreshList();
    }

    FastObservableCollection<ParkItemView> Attractions = new ();

    public ListAttractions()
    {
        FreezeParkList = true;
        InitializeComponent();

        AttractionList.ItemsSource = Attractions;
        AttractionList.RefreshCommand = new Command(() => OnListRefresh());
        AttractionListCompact.ItemsSource = Attractions;
        AttractionListCompact.RefreshCommand = new Command(() => OnListRefresh());
        ParkPicker.ItemsSource = Parks;

        this.BindingContext = this;



        UpdateTimer = Application.Current.Dispatcher.CreateTimer();
        UpdateTimer.Interval = TimeSpan.FromSeconds(30);
        UpdateTimer.Tick += async (s, e) => await RefreshList();
        UpdateTimer.IsRepeating = true;
        UpdateTimer.Start();
        FreezeParkList = false;
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
        if (RefresherMutex.CurrentCount == 0)
            return;
        await RefresherMutex.WaitAsync();

        var dt = DateTime.Now;

        IsRefreshing = true;
        OnPropertyChanged(nameof(IsRefreshing));

        if (forceRefresh)
            await DataManager.Instance.Update();

        IEnumerable<ParkItemView> newViewFilteredAndOrdered = new List<ParkItemView>();

        await Task.Run(async () =>
        {
            var staticdata = await DataManager.Instance.GetStaticDataBuffered();
            var rawlivedata = await DataManager.Instance.GetLiveDataBuffered();
            var livedata = new Dictionary<string, Model.LiveData>(rawlivedata);
            GeolocatorPlugin.Abstractions.Position? currentPosition;
            if (forceRefresh)
                currentPosition = await LocationManager.Instance.GetPositionAsync();
            else
                currentPosition = await LocationManager.Instance.GetPositionBuffered();

            var newView = new List<ParkItemView>();
            foreach (var parkelement in staticdata)
            {
                ParkItemView attraction_viewmodel = new(this)
                {
                    ParkElement = parkelement
                };

                var interestpoint = parkelement as InterestPoint;
                if (interestpoint != null && currentPosition != null && currentPosition.HasLatitudeLongitude && interestpoint.Position != null)
                {
                    Location here = new(currentPosition.Latitude, currentPosition.Longitude);
                    Location destpos = new(interestpoint.Position.Latitude, interestpoint.Position.Longitude);

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
            newViewFilteredAndOrdered = Sort(newViewFiltered);
        });

        if (!FreezeParkList)
        {
            Parks.SuspendCollectionChangeNotification();
            Parks.Clear();
            var parks = DataManager.Instance.BufferedParks.Select(x => x.Name).ToList();
            Parks.Add("Tous");
            Parks.AddItems(parks);
            Parks.ResumeCollectionChangeNotification();
            Parks.NotifyChanges();

            FreezeParkList = true;
            ParkPicker.SelectedIndex = DataManager.Instance.Configuration.FilterPark;
            FreezeParkList = false;
        }

        UpdateObservable(Attractions, newViewFilteredAndOrdered);

        Console.WriteLine($"Sorted dynamic data in {(DateTime.Now - dt).TotalMilliseconds}ms.");


        IsRefreshing = false;
        OnPropertyChanged(nameof(IsRefreshing));
        RefresherMutex.Release();
    }

    protected void UpdateObservable(FastObservableCollection<ParkItemView> view, IEnumerable<ParkItemView> list)
    {
        view.SuspendCollectionChangeNotification();
        view.Clear();
        foreach (var i in list)
            view.Add(i);
        view.ResumeCollectionChangeNotification();
        view.NotifyChanges();
    }

    protected IEnumerable<ParkItemView> Sort(IEnumerable<ParkItemView> attraction)
    {
        if (SortBy == SortMode.ByDistance)
            attraction = attraction.OrderBy(x => x.DistanceDbl);

        attraction = attraction.OrderBy(x => x.Favorite ? 0 : 1);

        if (InverseSort)
            attraction = attraction.Reverse();

        return attraction;
    }

    protected IEnumerable<ParkItemView> Filter(IEnumerable<ParkItemView> attraction)
    {
        if (FilterType == 1)
            attraction = attraction.Where(x => x.ParkElement is Model.Attraction);
        else if (FilterType == 2)
            attraction = attraction.Where(x => x.ParkElement is Show);
        else if (FilterType == 3)
            attraction = attraction.Where(x => x.ParkElement is Restaurant);

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

        if(FilterPark > 0 && FilterPark - 1 < DataManager.Instance.BufferedParks.Count)
        {
            string parkId = DataManager.Instance.BufferedParks[FilterPark-1].UniqueIdentifier;
            attraction = attraction.Where(x => x.ParkElement.ParkId == parkId);
        }

        return attraction;
    }

    private async void TypePicker_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (FreezeParkList)
            return;

        FreezeParkList = true;
        await RefreshList();
        FreezeParkList = false;
    }

    private async void AttractionList_ItemSelected(object sender, SelectedItemChangedEventArgs e)
    {
        ParkItemView item = e.SelectedItem as ParkItemView;
        if (item == null)
            return;

        var senderlst = sender as ListView;
        senderlst.SelectedItem = null;

        var page = new AttractionDetail();
        page.SetElement(item.ParkElement);
        await Navigation.PushAsync(page);
    }
}