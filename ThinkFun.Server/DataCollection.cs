using ThinkFun.Model;
using ThinkFun.Server.Sources.ThemeParkWiki;
using Destination = ThinkFun.Model.Destination;
using LiveData = ThinkFun.Model.LiveData;
using Park = ThinkFun.Model.Park;

namespace ThinkFun.Server;

public class DataCollection
{
    //By Id
    public Dictionary<string, Model.Destination> DestinationsbyId { get; private set; } = new ();
    public Dictionary<string, Model.Park> ParksbyId { get; private set; } = new ();
    public Dictionary<string, ParkElement> ParkElementsbyId { get; private set; } = new ();

    //by destination
    public Dictionary<string, HashSet<Model.Park>> ParksByDestinations { get; private set; } = new();
    public Dictionary<string, HashSet<Model.LiveData>> LiveDataByDestination { get; private set; } = new();
    public Dictionary<string, List<Event>> LastEventsByDestination { get; private set; } = new ();

    //by parks
    public Dictionary<string, HashSet<ParkElement>> PointsByParks { get; private set; } = new();

    public int MaxEventByDestination = 20;
      
    public IEnumerable<Destination> Destinations
    {
        get {  
            lock(DestinationsbyId)
                return DestinationsbyId.Values;
        }
    }

    public IEnumerable<LiveData> AllLiveData
    {
        get
        {
            foreach (var kv in LiveDataByDestination)
                foreach (var i in kv.Value)
                    yield return i;
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

    public EventsDestinationData? GetLastEvents(string destinationId)
    {
        if (destinationId == null) throw new ArgumentNullException(nameof(destinationId));

        lock (LastEventsByDestination)
        {
            EventsDestinationData ret = new();

            if (!LastEventsByDestination.ContainsKey(destinationId))
                return ret;

            foreach (var i in LastEventsByDestination[destinationId])
                if (i is StatusChangedEvent isce)
                    ret.StatusEvents.Add(isce);

            return ret;
        }
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
            string key = value.DestinationId;
            var id = value.UniqueIdentifier;
            bool keyExists = ParksByDestinations.ContainsKey(key);

            HashSet<Park> collection;
            if (keyExists)
                collection = ParksByDestinations[key];
            else
                collection = new HashSet<Park>();

            collection.Remove(value);
            collection.Add(value);

            if(!keyExists)
                ParksByDestinations.Add(key, collection);

            if (ParksbyId.ContainsKey(id))
                ParksbyId[id] = value;
            else
                ParksbyId.Add(id, value);
        }
    }

    public void Set(ParkElement value)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));

        lock (ParksByDestinations)
        {
            string key = value.ParkId;
            var id = value.UniqueIdentifier;
            bool keyExists = PointsByParks.ContainsKey(key);

            HashSet<ParkElement> collection;
            if (keyExists)
                collection = PointsByParks[key];
            else
                collection = new HashSet<ParkElement>();

            if (collection.Contains(value))
            {
                collection.Remove(value);
            }
            collection.Add(value);

            if (!keyExists)
                PointsByParks.Add(key, collection);

            if (ParkElementsbyId.ContainsKey(id))
                ParkElementsbyId[id] = value;
            else
                ParkElementsbyId.Add(id, value);
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

            if (collection.Contains(value))
            {
                if (collection.TryGetValue(value, out var last))
                    onReplace(last, value);
                collection.Remove(value);
            }
            collection.Add(value);

            if (!keyExists)
                LiveDataByDestination.Add(key, collection);


        }
    }

    protected void onReplace(LiveData previous, LiveData next)
    {
        var previousAsQueue = previous as Queue;
        var nextAsQueue = next as Queue;

        if (previousAsQueue == null || nextAsQueue == null)
            return;

        if (previousAsQueue.Status != nextAsQueue.Status)
            Add(new StatusChangedEvent
            {
                Date = DateTime.Now,
                DestinationId = next.DestinationId,
                ParkId = next.ParkId,
                ParkElementId = next.ParkElementId,
                OldStatus = previousAsQueue.Status,
                NewStatus = nextAsQueue.Status
            });

        /*if(previousAsQueue.Status == Status.OPENED
        && nextAsQueue.Status == Status.OPENED
        && previousAsQueue.SingleRiderWaitTime != nextAsQueue.SingleRiderWaitTime)
            DataManager.Instance.PushEvent(new WaitingTimeChangedEvent
            {
                Date = DateTime.Now,
                DestinationId = next.DestinationId,
                ParkId = next.ParkId,
                ParkElementId = next.ParkElementId,
                OldStatus = previousAsQueue.Status,
                NewStatus = nextAsQueue.Status
            });*/
    }

    protected void Add(Event e)
    {
        lock(LastEventsByDestination)
        {
            string key = e.DestinationId;

            bool keyExists = LastEventsByDestination.ContainsKey(key);
            
            List<Event> collection = keyExists ? LastEventsByDestination[key] : new();
            collection.Insert(0, e);
            if (!keyExists)
                LastEventsByDestination.Add(key, collection);

            if (collection.Count > MaxEventByDestination)
                collection.RemoveRange(MaxEventByDestination, collection.Count - MaxEventByDestination);
        }

        DataManager.Instance.PushEvent(e);
    }

    public Model.Destination? GetDestination(string id)
    {
        lock(DestinationsbyId)
        {
            if (DestinationsbyId.TryGetValue(id, out var destination))
                return destination;
        }

        return null;
    }

    public Model.Park? GetPark(string id)
    {
        lock (DestinationsbyId)
        {
            if (ParksbyId.TryGetValue(id, out var park))
                return park;
        }

        return null;
    }

    public Model.ParkElement? GetParkElement(string id)
    {
        lock (DestinationsbyId)
        {
            if (ParkElementsbyId.TryGetValue(id, out var elem))
                return elem;
        }

        return null;
    }


}
