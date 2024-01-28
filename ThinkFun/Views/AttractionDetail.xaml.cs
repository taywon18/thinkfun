using LiveChartsCore.SkiaSharpView;
using LiveChartsCore;
using System.ComponentModel;
using ThinkFun.Model;
using System.Collections.ObjectModel;
using System.Net.Http.Json;
using CommunityToolkit.Maui.Alerts;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using LiveChartsCore.Kernel;

namespace ThinkFun.Views;

public partial class AttractionDetail 
	: ContentPage
	, INotifyPropertyChanged
{
	ParkElement Element { get; set; } = null;

	public string ElementName
	{
		get 
		{ 
			if (Element == null)
				return "Nom 1";

			return Element.Name ?? "Nom 2";
		}
	}

    public ISeries[] Series { get; set; } = {};

    public SolidColorPaint LegendTextPaint { get; set; } =
    new SolidColorPaint
    {
        Color = new SKColor(50, 50, 50),
        //SKTypeface = SKTypeface.FromFamilyName("Courier New")
    };

    public SolidColorPaint LegendBackgroundPaint { get; set; } =
        new SolidColorPaint( new SKColor(240, 240, 240));



    public AttractionDetail()
	{
		InitializeComponent();
        this.BindingContext = this;
    }

	public async void SetElement(ParkElement element)
	{
        Element = element;

		OnPropertyChanged(nameof(ElementName));

        /*HistoryChart.XAxes = new[]
        {
            new DateTimeAxis(TimeSpan.FromHours(1), x => $"{x.Hour}h")
        };*/


        try
        {
            var Details = await DataManager.Instance.Client.GetFromJsonAsync<ParkElementDetail>($"Data/GetParkElementDetail/{element.DestinationId}/{element.ParkId}/{element.UniqueIdentifier}");

            LiveCharts.Configure(config =>
            {

            });

            HistoryChart.EasingFunction = null;
            HistoryChart.ZoomMode = LiveChartsCore.Measure.ZoomAndPanMode.None;

            var today = Details.Today.Points
                .Where(x => x.AverageWaitingTime.HasValue)
                .Select(x => new ObservablePoint(x.Begin.Hour + 0.5, x.AverageWaitingTime.Value.TotalMinutes));
            if (Details.LastMesure is not null && Details.LastMesure.ClassicWaitTime.HasValue)
                today = today.Append(
                    new ObservablePoint(
                        Details.LastMesure.LastUpdate.TimeOfDay.TotalHours
                        , Details.LastMesure.ClassicWaitTime.Value.TotalMinutes));

            var yesterday = Details.LastDay.Points.Where(x => x.AverageWaitingTime.HasValue).Select(x => new ObservablePoint(x.Begin.Hour + 0.5, x.AverageWaitingTime.Value.TotalMinutes));
            var lastweeksameday = Details.LastWeekSameDay.Points.Where(x => x.AverageWaitingTime.HasValue).Select(x => new ObservablePoint(x.Begin.Hour + 0.5, x.AverageWaitingTime.Value.TotalMinutes));

            //HistoryChart.XAxes.First().MinLimit = 8; 
            //HistoryChart.XAxes.First().MaxLimit = 22

            Series = new ISeries[]
            {
                new ColumnSeries<ObservablePoint>
                {
                    Values = yesterday,
                    Stroke = null,
                    MaxBarWidth = double.MaxValue,
                    IgnoresBarPosition = true,
                    Name = "Hier",
                    DataLabelsFormatter = (point) => point.Coordinate.PrimaryValue.ToString("F0") + "min",
                    DataLabelsSize = 8,
                    DataLabelsPaint = new SolidColorPaint(SKColors.White),
                    DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Middle,
                },

                new ColumnSeries<ObservablePoint>
                {
                    Values = lastweeksameday,
                    Stroke = null,
                    MaxBarWidth = 8,
                    IgnoresBarPosition = true,
                    Name = "J-7",
                    DataLabelsFormatter = (point) => point.Coordinate.PrimaryValue.ToString("F0") + "min",
                    DataLabelsSize = 5,
                    DataLabelsPaint = new SolidColorPaint(SKColors.Red),
                    DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Bottom,
                },

                new LineSeries<ObservablePoint>
                {
                    Values = today,
                    Stroke = null,
                    Name = "Aujourd'hui",
                    DataLabelsFormatter = (point) => point.Coordinate.PrimaryValue.ToString("F0") + "min",
                    DataLabelsSize = 8,
                    DataLabelsPaint = new SolidColorPaint(SKColors.Blue),
                    DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Top,

                }
            };

            OnPropertyChanged(nameof(Series));

        }catch(Exception ex)
        {
            var snackbar = Snackbar.Make($"Erreur serveur: {ex}");
            await snackbar.Show();
        }

    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        /*// Pie Chart Series
        SeriesChart = new ObservableCollection<ISeries>();

        PieSeries<double?> aSeries;

        aSeries = new PieSeries<double?> { Values = new List<double?> { 10 }, Name = "Value 1" };
        SeriesChart.Add(aSeries);

        aSeries = new PieSeries<double?> { Values = new List<double?> { 5 }, Name = "Value 2" };
        SeriesChart.Add(aSeries);

        // Note : Replace the :) by a real emoji, somehow, Stackoverflow tells me the code is invalid if I post a real emoji
        aSeries = new PieSeries<double?> { Values = new List<double?> { 5 }, Name = "Emoji :) (Not Working)" };
        SeriesChart.Add(aSeries);

        // UI : Bind the chart
        crtPieEmojisSent.Series = SeriesChart;*/
    }
}