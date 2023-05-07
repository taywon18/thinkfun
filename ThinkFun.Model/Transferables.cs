namespace ThinkFun.Model;

public class StaticDestinationData
{
    public Destination Destination { get; set; } 
    public List<Park> Parks { get; set; } = new List<Park>();
    public List<Attraction> Attractions { get; set; } = new List<Attraction>();
    public List<Restaurant> Restaurants { get; set; } = new List<Restaurant>();
    public List<Show> Shows { get; set; } = new List<Show>();
}

public class LiveDestinationData
{
    public List<Queue> Queues { get; set; } = new List<Queue>();
}

public class EventsDestinationData
{
    public List<StatusChangedEvent> StatusEvents { get; set; } = new ();
}