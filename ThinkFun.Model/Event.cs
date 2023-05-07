using ThinkFun.Model;

namespace ThinkFun.Model;

public class Event
{
    public DateTime Date = DateTime.Now;
    public string DestinationId;
    public string ParkId;
    public string ParkElementId;

    public string UniqueId;

    public Event()
    {
        UniqueId = Guid.NewGuid().ToString();
    }
}

public class StatusChangedEvent
    : Event
{
    public Status OldStatus;
    public Status NewStatus;
}