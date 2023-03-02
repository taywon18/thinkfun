using Mapsui.UI.Maui;

namespace ThinkFun.Views;

public partial class Map : ContentPage
{
    MapControl MapControl;

    public Map()
	{
		InitializeComponent();

        MapControl = new Mapsui.UI.Maui.MapControl();
        MapControl.Map?.Layers.Add(Mapsui.Tiling.OpenStreetMap.CreateTileLayer());
        Content = MapControl;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
    }
}