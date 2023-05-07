namespace ThinkFun.Server;

using MongoDB.Bson;
using MongoDB.Driver;
using ThinkFun.Model;
using Wood;

public class DataStore
{
    public static DataStore Instance { get;  } = new DataStore();

    private IMongoCollection<LiveData> LiveDataCollection;
    private IMongoCollection<Event> EventCollection;

    private string ConnectionString = "";
    private string DatabaseName = "";
    private string LiveDataCollectionName = "";
    private string EventCollectionName = "";


    public DataStore()
    {

    }

    public async Task Configure(IConfiguration conf, CancellationToken tk = default)
    {
        if(conf[nameof(ConnectionString)] == null)
        {
            LogManager.Error($"Cannot configure storage without database {nameof(ConnectionString)} value.");
            return;
        }

        if (conf[nameof(DatabaseName)] == null)
        {
            LogManager.Error($"Cannot configure storage without database {nameof(DatabaseName)} value.");
            return;
        }

        if (conf[nameof(LiveDataCollectionName)] == null)
        {
            LogManager.Error($"Cannot configure storage without database {nameof(LiveDataCollectionName)} value.");
            return;
        }

        if (conf[nameof(EventCollectionName)] == null)
        {
            LogManager.Error($"Cannot configure storage without database {nameof(EventCollectionName)} value.");
            return;
        }

        var mongoClient = new MongoClient(conf[nameof(ConnectionString)]);
        var mongoDatabase = mongoClient.GetDatabase(conf[nameof(DatabaseName)]);
        LiveDataCollection = mongoDatabase.GetCollection<LiveData>(conf[nameof(LiveDataCollectionName)]);
        EventCollection = mongoDatabase.GetCollection<Event>(conf[nameof(EventCollectionName)]);

        await LiveDataCollection.Indexes.CreateOneAsync(
            Builders<LiveData>.IndexKeys.Descending(x => x.LastUpdate).Ascending(x => x.ParkElementId),
            new CreateIndexOptions() { Unique = true},
            tk);
    }


    public async IAsyncEnumerable<LiveData> Get(string element, DateTime from, DateTime to, CancellationToken token = default)
    {
        var res = await LiveDataCollection.Find(x 
            => x.ParkElementId == element 
            && x.LastUpdate >= from 
            && x.LastUpdate < to
            )
            .ToCursorAsync(token);
        while (await res.MoveNextAsync(token))
        {
            if(token.IsCancellationRequested) 
                yield break;

            foreach (var i in res.Current)
                yield return i;
        }
    }

    public async Task Add(IEnumerable<LiveData> ld, CancellationToken token = default)
    {
        try
        {
            await LiveDataCollection.InsertManyAsync(ld, new InsertManyOptions
            {
                IsOrdered = false
            }, cancellationToken: token);
        }
        catch(MongoBulkWriteException bex)
        {
            //LogManager.Debug($"Skipping error for bulk mongodb insert {bex}");
        }
    }

    public async Task Add(Event e, CancellationToken tk = default)
    {
        await EventCollection.InsertOneAsync(e, tk);
    }
}
