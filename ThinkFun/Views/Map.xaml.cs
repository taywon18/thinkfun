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

namespace ThinkFun.Views;

public partial class Map : ContentPage
{
    MapControl MapControl;
    Pin WhereIAm = null;
    MemoryLayer RenderLayer = null;

    public Map()
	{
		InitializeComponent();

        MapControl = new Mapsui.UI.Maui.MapControl();
        MapControl.Map?.Layers.Add(Mapsui.Tiling.OpenStreetMap.CreateTileLayer());
        Content = MapControl;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await DrawGraph();
    }

    async Task DrawGraph()
    {
        WhereIAm = new Pin(new MapView())
        {
            Position = new Mapsui.UI.Maui.Position(48.8674, 2.7836),
            Scale = 0.3f,
            Label = "LL"
        };

        var osmlayer = Mapsui.Tiling.OpenStreetMap.CreateTileLayer();
        MapControl.Map?.Layers.Add(osmlayer);

        RenderLayer = new MemoryLayer
        {
            Style = null,
            Features = await GetListOfPoints(),
            Name = "LABELS",
            IsMapInfoLayer = true
        };
        MapControl.Map?.Layers.Add(RenderLayer);

        //MapControl.Pins.Add(WhereIAm);

        MapControl.Map.Home = x => x.NavigateTo((new Position(48.8674, 2.7836)).ToMapsui(), 1);
        MapControl.Map.Info += MapInfoAsk;
    }

    private async void MapInfoAsk(object sender, Mapsui.UI.MapInfoEventArgs e)
    {
        var featureLabel = e.MapInfo.Feature?["ParkElement"] as ParkElement;
        if (featureLabel != null)
        {
            Debug.WriteLine("Info Event was invoked.");
            Debug.WriteLine("Feature id: " + featureLabel);
            await DisplayAlert("Attraction", featureLabel.Name, "OK");
        }
    }

    async Task<IEnumerable<IFeature>> GetListOfPoints()
    {
        List<IFeature> list = new List<IFeature>();


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



     

        IEnumerable<IFeature> points = list as IEnumerable<IFeature>;
        return points;
    }
}