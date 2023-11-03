using LiveChartsCore.SkiaSharpView;
using LiveChartsCore;
using System.ComponentModel;
using ThinkFun.Model;
using System.Collections.ObjectModel;

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

    public ISeries[] Series { get; set; } =
{
        new ColumnSeries<int>
        {
            Values = new[] { 6, 3, 5, 7, 3, 4, 6, 3 },
            Stroke = null,
            MaxBarWidth = double.MaxValue,
            IgnoresBarPosition = true
        },
        new ColumnSeries<int>
        {
            Values = new[] { 2, 4, 8, 9, 5, 2, 4, 7 },
            Stroke = null,
            MaxBarWidth = 30,
            IgnoresBarPosition = true
        }
    };

    public AttractionDetail()
	{
		InitializeComponent();
        this.BindingContext = this;
    }

	public void SetElement(ParkElement element)
	{
        Element = element;

		OnPropertyChanged(nameof(ElementName));

		LiveCharts.Configure(config =>
		{

		});
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