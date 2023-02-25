using ThinkFun.Model;

namespace ThinkFun.Server;

public class DataCollection
{
    public Dictionary<string, Destination> DestinationsbyId { get; private set; } = new Dictionary<string, Destination>();
    public Dictionary<string, HashSet<Park>> ParksByDestinations { get; private set; } = new Dictionary<string, HashSet<Park>>();
    public Dictionary<string, HashSet<ParkElement>> PointsByParks { get; private set; } = new Dictionary<string, HashSet<ParkElement>>();

    public Dictionary<string, HashSet<LiveData>> LiveDataByDestination { get; private set; } = new Dictionary<string, HashSet<LiveData>>();

    public IEnumerable<Destination> Destinations
    {
        get {  
            lock(DestinationsbyId)
                return DestinationsbyId.Values;
        }
    }

    public StaticDestinationData? GetStaticData(string destinationId)
    {
        if (destinationId == null) throw new ArgumentNullException(nameof(destinationId));

        Destination destination;
        lock(DestinationsbyId)
        {
            if (!DestinationsbyId.ContainsKey(destinationId))
                return null;
            destination = DestinationsbyId[destinationId];
        }

        var parks = new List<Park>();
        lock(ParksByDestinations)
        {
            if (ParksByDestinations.ContainsKey(destinationId))
                parks.AddRange(ParksByDestinations[destinationId]);
        }

        var attractions = new List<Attraction>();
        var shows = new List<Show>();
        var restaurants = new List<Restaurant>();
        foreach(var p in parks)
            lock (PointsByParks)
                if (PointsByParks.ContainsKey(p.UniqueIdentifier))
                    foreach(var elem in PointsByParks[p.UniqueIdentifier])
                        if (elem is Attraction)
                            attractions.Add((Attraction)elem);
                        else if(elem is Show)
                            shows.Add((Show)elem); 
                        else if(elem is Restaurant)
                            restaurants.Add((Restaurant)elem);

        return new StaticDestinationData
        {
            Destination = destination,
            Parks = parks,
            Attractions = attractions,
            Shows = shows,
            Restaurants = restaurants
        };
    }


    public LiveDestinationData? GetLiveData(string destinationId)
    {
        if (destinationId == null) throw new ArgumentNullException(nameof(destinationId));

        List<LiveData> livedatas;
        lock (LiveDataByDestination)
        {
            if (!LiveDataByDestination.ContainsKey(destinationId))
                return null;
            livedatas = new List<LiveData>(LiveDataByDestination[destinationId]);
        }

        List<Queue> queues = new List<Queue>();
        foreach(var i in livedatas)
            if(i is Queue)
                queues.Add((Queue)i);

        return new LiveDestinationData
        {
            Queues = queues
        };
    }


    public void Set(Destination value)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));

        lock (DestinationsbyId)
        {
            DestinationsbyId[value.UniqueIdentifier] = value;
        }
    }
    
    public void Set(Park value)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));

        lock (ParksByDestinations)
        {
            string key = value.ParentId;
            bool keyExists = ParksByDestinations.ContainsKey(key);

            HashSet<Park> collection;
            if (keyExists)
                collection = ParksByDestinations[key];
            else
                collection = new HashSet<Park>();

            collection.Add(value);

            if(!keyExists)
                ParksByDestinations.Add(key, collection);
        }
    }

    public void Set(ParkElement value)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));

        lock (ParksByDestinations)
        {
            string key = value.ParentId;
            bool keyExists = PointsByParks.ContainsKey(key);

            HashSet<ParkElement> collection;
            if (keyExists)
                collection = PointsByParks[key];
            else
                collection = new HashSet<ParkElement>();

            collection.Add(value);

            if (!keyExists)
                PointsByParks.Add(key, collection);
        }
    }

    public void Add(LiveData value)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));

        lock (ParksByDestinations)
        {
            string key = value.DestinationId;
            bool keyExists = LiveDataByDestination.ContainsKey(key);

            HashSet<LiveData> collection;
            if (keyExists)
                collection = LiveDataByDestination[key];
            else
                collection = new HashSet<LiveData>();

            collection.Add(value);

            if (!keyExists)
                LiveDataByDestination.Add(key, collection);
        }
    }
}
