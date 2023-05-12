using ThinkFun.Model;

namespace ThinkFun.Model;

public class Event
{
    public DateTime Date { get; set; } = DateTime.Now;
    public string DestinationId { get; set; }
    public string ParkId { get; set; }
    public string ParkElementId { get; set; }

    public string UniqueId { get; set; }

    public Event()
    {
        UniqueId = Guid.NewGuid().ToString();
    }
}

public class StatusChangedEvent
    : Event
{
    public Status OldStatus { get; set; }
    public Status NewStatus { get; set; }
}