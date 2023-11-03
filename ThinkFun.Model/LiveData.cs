using System;
using System.Text.Json.Serialization;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ThinkFun.Model;

public enum Status
{
    OPENED,
    DOWN,
    CLOSED
}

public abstract class LiveData
{
    static public TimeSpan DefaultRound = TimeSpan.FromMinutes(5);

    public string DestinationId { get; set; }
    public string ParkId { get; set; }
    public string ParkElementId { get; set; }
    public DateTime LastUpdate { get; set; } = DateTime.Now;

    public DateTime RoundedLastUpdate { get; set; }
    public TimeSpan RoundScale { get; set; }

    public void Round()
    {
        Round(DefaultRound);
    }

    public void Round(TimeSpan interval)
    {
        long ticks = (LastUpdate.Ticks / interval.Ticks);
        RoundedLastUpdate = new DateTime(ticks * interval.Ticks);
        RoundScale = interval;
    }

    public override int GetHashCode()
    {
        return ParkElementId.GetHashCode();
    }

    public override bool Equals(object? obj)
    {

        if (obj == null)
            return false;

        if (this.GetType() != obj.GetType())
            return false;


        var objAsIp = obj as LiveData;
        if (objAsIp == null)
            return false;

        return ParkElementId == objAsIp.ParkElementId;
    }
}


public class Queue
    : LiveData
{
    public TimeSpan? ClassicWaitTime { get; set; } = null;
    public TimeSpan? SingleRiderWaitTime { get; set; } = null;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Status Status { get; set; }
}