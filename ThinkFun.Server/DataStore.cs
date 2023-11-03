namespace ThinkFun.Server;

using DevOne.Security.Cryptography.BCrypt;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using ThinkFun.Model;
using Wood;

public class DataStore
{
    public static DataStore Instance { get;  } = new DataStore();

    private IMongoCollection<LiveData> LiveDataCollection;
    private IMongoCollection<Event> EventCollection;
    private IMongoCollection<User> UserCollection;
    private IMongoCollection<HistoryArray> HistoryCollection;

    private string ConnectionString = "";
    private string DatabaseName = "";
    private string LiveDataCollectionName = "";
    private string EventCollectionName = "";
    private string UserCollectionName = "";
    private string HistoryCollectionName = "";

    private string PasswordSalt = String.Empty; //TODO: load

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

        if (conf[nameof(HistoryCollectionName)] == null)
        {
            LogManager.Error($"Cannot configure storage without database {nameof(HistoryCollectionName)} value.");
            return;
        }

        if (conf[nameof(EventCollectionName)] == null)
        {
            LogManager.Error($"Cannot configure storage without database {nameof(EventCollectionName)} value.");
            return;
        }
        
        if (conf[nameof(UserCollectionName)] == null)
        {
            LogManager.Error($"Cannot configure storage without database {nameof(UserCollectionName)} value.");
            return;
        }

        var mongoClient = new MongoClient(conf[nameof(ConnectionString)]);
        var mongoDatabase = mongoClient.GetDatabase(conf[nameof(DatabaseName)]);
        LiveDataCollection = mongoDatabase.GetCollection<LiveData>(conf[nameof(LiveDataCollectionName)]);
        EventCollection = mongoDatabase.GetCollection<Event>(conf[nameof(EventCollectionName)]);
        UserCollection = mongoDatabase.GetCollection<User>(conf[nameof(UserCollectionName)]);
        HistoryCollection = mongoDatabase.GetCollection<HistoryArray>(conf[nameof(HistoryCollectionName)]);

        await LiveDataCollection.Indexes.CreateOneAsync(
            Builders<LiveData>.IndexKeys.Descending(x => x.LastUpdate).Ascending(x => x.ParkElementId),
            new CreateIndexOptions() { Unique = true},
            tk);


        await UserCollection.Indexes.CreateOneAsync(
            Builders<User>.IndexKeys.Descending(x => x.Name),
            new CreateIndexOptions() { Unique = true },
            tk);


        if (conf[nameof(PasswordSalt)] != null)
        {
            PasswordSalt = conf[nameof(PasswordSalt)];
        }
        else
            LogManager.Alert($"No password salt defined... Use this one: {BCryptHelper.GenerateSalt(10)}");

    }


    public async IAsyncEnumerable<LiveData> Get(/*string destination, string park,*/ string element, DateTime from, DateTime to, CancellationToken token = default)
    {
        var res = await LiveDataCollection.Find(x 
            => x.ParkElementId == element
            /*&& x.ParkId == park
            && x.DestinationId == destination*/
            && x.RoundedLastUpdate >= from 
            && x.RoundedLastUpdate < to
            )
            .SortBy(x => x.RoundedLastUpdate)
            .ToCursorAsync(token);

        while (await res.MoveNextAsync(token))
        {
            if(token.IsCancellationRequested) 
                yield break;

            foreach (var i in res.Current)
                yield return i;
        }
    }

    public async Task<HistoryArray> GetHistory(string element, DateTime from, DateTime to, TimeSpan period, CancellationToken token = default)
    {
        throw new NotImplementedException();
        
        from = from.Ceil(period);
        to = to.Ceil(period);


        await foreach(var i in Get(element, from, to, token))
        {

        }
    }


    public async Task Add(IEnumerable<LiveData> ld, CancellationToken token = default)
    {
        var lst = ld.ToList();
        if (lst.Count == 0)
            return;

        long err = 0;

        try
        {
            await LiveDataCollection.InsertManyAsync(lst, new InsertManyOptions
            {
                IsOrdered = false
            }, cancellationToken: token);
        }
        catch(MongoBulkWriteException bex)
        {
            //LogManager.Debug($"Skipping error for bulk mongodb insert {bex}");
            err = bex.WriteErrors.Count;
        }

        LogManager.Debug($"Skipping {err} ({((float)(err))/lst.Count*100}%) error for bulk mongodb insert.");
    }

    public async Task Add(Event e, CancellationToken tk = default)
    {
        await EventCollection.InsertOneAsync(e, tk);
    }

    public async Task<User> Register(RegisterRequest req, CancellationToken tk = default)
    {
        string hash = DevOne.Security.Cryptography.BCrypt.BCryptHelper.HashPassword(req.Password, PasswordSalt);
        var user = new User
        {
            Identifier = Guid.NewGuid().ToString(),
            Name = req.Name,
            PasswordHash = hash,
        };

        await UserCollection.InsertOneAsync(user, cancellationToken: tk);

        return user;
    }

    public async Task<User?> Login(LoginRequest req, CancellationToken tk = default)
    {
        var cursor = await UserCollection.FindAsync(x => x.Name == req.Name, cancellationToken: tk);
        if (!await cursor.MoveNextAsync(tk) || !cursor.Current.Any())
            return null;

        var user = cursor.Current.First();
        if (user.PasswordHash != DevOne.Security.Cryptography.BCrypt.BCryptHelper.HashPassword(req.Password, PasswordSalt))
            return null;

        return user;
    }

    public async Task<User> GetUser(string username, CancellationToken tk = default)
    {
        var cursor = await UserCollection.FindAsync(x => x.Name == username, cancellationToken: tk);
        if (!await cursor.MoveNextAsync(tk) || !cursor.Current.Any())
            return null;

        var user = cursor.Current.First();
        user.PasswordHash = "";

        return user;
    }
}
