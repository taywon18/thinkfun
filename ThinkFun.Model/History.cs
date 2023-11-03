namespace ThinkFun.Model;

public class HistoryArray
{
    public string DestinationId { get; set; }
    public string ParkId { get; set; }
    public string ParkElementId { get; set; }

    public DateTime LastUpdate { get; set; } = DateTime.Now;
    public DateTime Begin { get; set; }
    public TimeSpan End { get; set; }

    public List<HistoryPoint> Points { get; set; } = new ();
}

public class HistoryPoint
{
    public DateTime Begin { get; set; }
    public TimeSpan Duration { get; set; }

    public TimeSpan MinimumWaitingTime { get; set; }
    public TimeSpan MaximumWaitingTime { get; set; }
    public TimeSpan MedianWaitingTime { get; set; }
}
