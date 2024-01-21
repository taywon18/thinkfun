using LiveChartsCore.SkiaSharpView;
using LiveChartsCore;
using System.ComponentModel;
using ThinkFun.Model;
using System.Collections.ObjectModel;
using System.Net.Http.Json;
using CommunityToolkit.Maui.Alerts;
using LiveChartsCore.Defaults;

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

            var today = Details.Today.Points
                .Where(x => x.AverageWaitingTime.HasValue)
                .Select(x => new ObservablePoint(x.Begin.Hour + 0.5, x.AverageWaitingTime.Value.TotalMinutes));
            if (Details.LastMesure is not null)
                today = today.Append(
                    new ObservablePoint(
                        Details.LastMesure.FirstMesure.Hour + Details.LastMesure.FirstMesure.Minute/60
                        , Details.LastMesure.AverageWaitingTime.Value.TotalMinutes ));

            var yesterday = Details.LastDay.Points.Where(x => x.AverageWaitingTime.HasValue).Select(x => new ObservablePoint(x.Begin.Hour + 0.5, x.AverageWaitingTime.Value.TotalMinutes));
            var lastweeksameday = Details.LastWeekSameDay.Points.Where(x => x.AverageWaitingTime.HasValue).Select(x => new ObservablePoint(x.Begin.Hour + 0.5, x.AverageWaitingTime.Value.TotalMinutes));

            //HistoryChart.XAxes.First().MinLimit = 8; 
            //HistoryChart.XAxes.First().MaxLimit = 22; 

            Series = new ISeries[]
            {
                new ColumnSeries<ObservablePoint>
                {
                    Values = yesterday,
                    Stroke = null,
                    MaxBarWidth = double.MaxValue,
                    IgnoresBarPosition = true
                },

                new ColumnSeries<ObservablePoint>
                {
                    Values = lastweeksameday,
                    Stroke = null,
                    MaxBarWidth = 30,
                    IgnoresBarPosition = true
                },

                new LineSeries<ObservablePoint>
                {
                    Values = today,
                    Stroke = null
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