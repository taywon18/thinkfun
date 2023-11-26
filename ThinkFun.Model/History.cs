namespace ThinkFun.Model;

public class HistoryArray
{
    public string DestinationId { get; set; }
    public string ParkId { get; set; }
    public string ParkElementId { get; set; }

    public DateTime LastUpdate { get; set; } = DateTime.Now;
    public DateTime Begin { get; set; }
    public DateTime End { get; set; }
    public DateTime? FirstMesure { 
        get
        {
            if(Points.Count == 0) 
                return null;
            
            return Points.Min(x => x.FirstMesure);
        }
    }

    public DateTime? LastMesure
    {
        get
        {
            if (Points.Count == 0)
                return null;

            return Points.Max(x => x.LastMesure);
        }
    }

    public TimeSpan Period { get; set; }

    public List<HistoryPoint> Points { get; set; } = new ();
}

public class HistoryPoint
{
    public DateTime Begin { get; set; }
    public TimeSpan Duration { get; set; }

    public DateTime FirstMesure { get; set; }
    public DateTime LastMesure { get; set; }
    public int SamplesCount { get; set; }

    public TimeSpan? MinimumWaitingTime { get; set; }
    public TimeSpan? MaximumWaitingTime { get; set; }
    //public TimeSpan? MedianWaitingTime { get; set; }
    public TimeSpan? AverageWaitingTime { get; set; }
    public double OperatingTime { get; set; }
}
