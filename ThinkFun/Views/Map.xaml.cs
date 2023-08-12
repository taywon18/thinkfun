using Mapsui;
using Mapsui.Layers;
using Mapsui.Styles;
using Mapsui.UI.Maui;
using Mapsui.Projections;
using Color = Mapsui.Styles.Color;
using Brush = Mapsui.Styles.Brush;
using ThinkFun.Model;
using Position = Mapsui.UI.Maui.Position;
using System.Diagnostics;
using HarfBuzzSharp;
using Mapsui.Providers;
using System.Threading;
using CommunityToolkit.Maui.Alerts;

namespace ThinkFun.Views;

public partial class Map : ContentPage
{
    MapControl MapControl;
    Layer RenderLayer = null;
    Mapsui.Tiling.Layers.TileLayer OsmLayer;
    IDispatcherTimer Timer;

    public Map()
	{
		InitializeComponent();
        OsmLayer = Mapsui.Tiling.OpenStreetMap.CreateTileLayer();

        Timer = Application.Current.Dispatcher.CreateTimer();
        Timer.Interval = TimeSpan.FromSeconds(0.5);
        Timer.Tick += (s, e) => MapControl.RefreshData();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        MapControl = new Mapsui.UI.Maui.MapControl();
        Content = MapControl;

        MapControl.Map.Home = x => x.CenterOn((new Position(48.8674, 2.7836)).ToMapsui(), 1);
        MapControl.Map.Info += MapInfoAsk;

        await DrawGraph();
        Timer.Start();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        Timer.Stop();
    }

    async Task DrawGraph()
    {
        MapControl.Map.Layers.Clear();
        
        MapControl.Map.Layers.Add(OsmLayer);
        RenderLayer = new Layer
        {
            Style = null,
            DataSource = new IconProvider(this),
            Name = "LABELS",
            IsMapInfoLayer = true
        };
        MapControl.Map.Layers.Add(RenderLayer);


        var poslayer = new Layer
        {
            Style = null,
            DataSource = new LocationProvider(),
            Name = "POS",
            IsMapInfoLayer = true
        };
        MapControl.Map.Layers.Add(poslayer);

        MapControl.Map.Navigator.CenterOn((new Position(48.8674, 2.7836)).ToMapsui(), 1);
        MapControl.Refresh();
    }

    private async void MapInfoAsk(object sender, Mapsui.MapInfoEventArgs e)
    {
        var featureLabel = e.MapInfo.Feature?["ParkElement"] as ParkElement;
        if (featureLabel != null)
        {
            Debug.WriteLine("Info Event was invoked.");
            Debug.WriteLine("Feature id: " + featureLabel);
            await DisplayAlert("Attraction", featureLabel.Name, "OK");
        }
    }

    class IconProvider
        : Mapsui.Providers.IProvider
    {
        public string CRS;
        string IProvider.CRS { get => CRS; set => CRS = value; }
        Map Map;
        
        public IconProvider(Map map)
        {
            Map = map;
        }

        public MRect GetExtent()
        {
            return null;
        }

        public Task<IEnumerable<IFeature>> GetFeaturesAsync(FetchInfo fetchInfo)
        {
            return Map.GetListOfPoints();
        }
    }

    class LocationProvider
        : Mapsui.Providers.IProvider
    {
        public string CRS;
        string IProvider.CRS { get => CRS; set => CRS = value; }


        public MRect GetExtent()
        {
            return null;
        }

        public async Task<IEnumerable<IFeature>> GetFeaturesAsync(FetchInfo fetchInfo)
        {
            var features = new List<IFeature>();
            var style = new Mapsui.Styles.LabelStyle
            {
                Text = "Mapos",
                BackColor = new Mapsui.Styles.Brush(Color.Blue),
                ForeColor = Color.White,
            };

            var pos = await LocationManager.Instance.GetPositionBuffered();
            if (pos == null)
                return features;

            var feature = new PointFeature((new Mapsui.UI.Maui.Position(pos.Latitude, pos.Longitude)).ToMapsui())
            {
                Styles = new[] { style }
            };
            features.Add(feature);
            return features;
        }
    }

    async Task<IEnumerable<IFeature>> GetListOfPoints()
    {
        List<IFeature> list = new ();

        var staticData = await DataManager.Instance.GetStaticDataBuffered();
        var rawliveData = await DataManager.Instance.GetLiveDataBuffered();
        var livedata = new Dictionary<string, Model.LiveData>(rawliveData);
        foreach (var i in staticData)
        {
            var attraction = i as Attraction;
            if(attraction == null) continue;

            if (!livedata.ContainsKey(i.UniqueIdentifier))
                continue;
            var live = livedata[i.UniqueIdentifier];

            var queue = live as Queue;
            if(queue == null)
                continue;

            Color color;
            if (queue.Status == Status.OPENED)
                color = Color.Green;
            else if (queue.Status == Status.DOWN)
                color = Color.Orange;
            else
                continue;

            string text = "?";
            if(queue.ClassicWaitTime.HasValue)
                text = String.Format("{0:0} min", queue.ClassicWaitTime.Value.TotalMinutes);

            if (DataManager.Instance.Configuration.FavoriteElements.Contains(i.UniqueIdentifier))
                text = "☺ " + text;

            var label = new LabelStyle
            {
                Text = text,
                BackColor = new Mapsui.Styles.Brush(color),
                ForeColor = Color.White,
            };

            List<Mapsui.Styles.IStyle> style = new()
            {
                label
            };

            var feature = new PointFeature((new Mapsui.UI.Maui.Position(attraction.Position.Latitude, attraction.Position.Longitude)).ToMapsui())
            {
                Styles = style
            };
            feature["ParkElement"] = i;

            list.Add(feature);
        }

        return list as IEnumerable<IFeature>;
    }

    private async void ImageButton_Clicked(object sender, EventArgs e)
    {
        await LocationManager.Instance.StartListening();
    }
}