using ThinkFun.Model;

namespace ThinkFun.Views;

public partial class EventList 
    : ContentPage
{
    public List<RichEvent> Events
    {
        get
        {
            return DataManager.Instance.BufferedEvents;
        }
    }

    public EventList()
    {
        InitializeComponent();

        this.BindingContext = this;
        EventsList.ItemsSource = Events;
    }
}