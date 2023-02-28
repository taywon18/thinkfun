namespace ThinkFun.Server.Sources.ThemeParkWiki;

public class Location
{
    public double latitude { get; set; }
    public double longitude { get; set; }
}

public class Entity
{
    public string id { get; set; }
    public string name { get; set; }
    public string entityType { get; set; }
    public string parentId { get; set; }
    public string destinationId { get; set; }
    public string timezone { get; set; }
    public string externalId { get; set; }
    public Location location { get; set; }

}
public class Park
{
    public string id { get; set; }
    public string name { get; set; }
}

public class Destination
{
    public string id { get; set; }
    public string name { get; set; }
    public string slug { get; set; }
    public List<Park> parks { get; set; } = new List<Park>();
}

public class DestinationList
{
    public IList<Destination> destinations { get; set; } = new List<Destination>();
}

public class Child
{
    public string id { get; set; }
    public string name { get; set; }
    public string entityType { get; set; }
}

public class ChildenList
{
    public IList<Child> children { get; set; } = new List<Child>();
}

public class LiveDataPayingQueuePrice
{
    public int amount { get; set; } //in cents
    public string currency { get; set; }
}

public class LiveDataPayingQueue
{
    public LiveDataPayingQueuePrice price { get; set; }
    public string state { get; set; }
    public string returnStart { get; set; }
    public string returnEnd { get; set; }
}

public class LiveDataQueue
{
    public int? waitTime { get; set; } //minutes
}   

public class LiveDataQueueList
{
    public LiveDataQueue? STANDBY { get; set; }
    public LiveDataQueue? SINGLE_RIDER { get; set; }
    public LiveDataPayingQueue? PAID_RETURN_TIME { get; set; }
    
}

public class LiveData
{
    public string id { get; set; }
    public string name { get; set; }
    public string entityType { get; set; }
    public string parkId { get; set; }
    public string externalId { get; set; }
    public DateTime lastUpdated { get; set; }
    public string status { get; set; }
    public LiveDataQueueList? queue { get; set; }
}

public class LiveDataResponse
{
    public List<LiveData> liveData { get; set; } = new List<LiveData>();
}
