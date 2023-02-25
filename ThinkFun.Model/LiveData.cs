using System.Text.Json.Serialization;

namespace ThinkFun.Model;

public enum Status
{
    OPENED,
    DOWN,
    CLOSED
}

public abstract class LiveData
{
    public string DestinationId { get; set; }
    public string ParkId { get; set; }
    public string ParkElementId { get; set; }
    public DateTime LastUpdate { get; set; } = DateTime.Now;

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