namespace ThinkFun.Server;

using DevOne.Security.Cryptography.BCrypt;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Data;
using System.Security.Claims;
using ThinkFun.Model;
using Wood;

public class DataStore
{
    public static DataStore Instance { get;  } = new DataStore();

    private IMongoDatabase MongoDatabase;
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
        MongoDatabase = mongoClient.GetDatabase(conf[nameof(DatabaseName)]);
        LiveDataCollection = MongoDatabase.GetCollection<LiveData>(conf[nameof(LiveDataCollectionName)]);
        EventCollection = MongoDatabase.GetCollection<Event>(conf[nameof(EventCollectionName)]);
        UserCollection = MongoDatabase.GetCollection<User>(conf[nameof(UserCollectionName)]);
        HistoryCollection = MongoDatabase.GetCollection<HistoryArray>(conf[nameof(HistoryCollectionName)]);


        try
        {
            await LiveDataCollection.Indexes.CreateOneAsync(
                Builders<LiveData>
                .IndexKeys
                .Descending(x => x.RoundedLastUpdate)
                .Ascending(x => x.DestinationId)
                .Ascending(x => x.ParkId)
                .Ascending(x => x.ParkElementId)
                , new CreateIndexOptions() { Unique = true },
            tk);
        } 
        catch(Exception ex)
        {
            LogManager.Alert($"Index by update desc & parkelement asc creation failed: {ex}");
        }


        try
        {
            await UserCollection.Indexes.CreateOneAsync(
                Builders<User>.IndexKeys.Descending(x => x.Name),
                new CreateIndexOptions() { Unique = true },
            tk);
        }
        catch (Exception ex)
        {
            LogManager.Alert($"Index by name desc creation failed: {ex}");
        }



        if (conf[nameof(PasswordSalt)] != null)
        {
            PasswordSalt = conf[nameof(PasswordSalt)];
        }
        else
            LogManager.Alert($"No password salt defined... Use this one: {BCryptHelper.GenerateSalt(10)}");

        if (await CheckIsConnected())
            LogManager.Information($"Connection test ok");
        else
            LogManager.Alert("Connection test failed.");

    }


    public async IAsyncEnumerable<LiveData> Get(string destination, string park, string element, DateTime from, DateTime to, CancellationToken token = default)
    {
        var res = await LiveDataCollection.Find(x 
            => x.ParkElementId == element
            && x.ParkId == park
            && x.DestinationId == destination
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

    public async Task<HistoryArray> GetHistory(string destination, string park, string element, DateTime from, TimeSpan duration, TimeSpan period, CancellationToken token = default)
    {
        return await GetHistory(destination, park, element, from, from + duration - TimeSpan.FromTicks(1), period, token);
    }

    public async Task<HistoryArray> GetHistory(string destination, string park, string element, DateTime from, DateTime to, TimeSpan period, CancellationToken token = default)
    {        
        from = from.Ceil(period);
        to = to.Ceil(period);
        var lst = await Get(destination, park, element, from, to, token).ToListAsync();
        lst.OrderBy(x => x.RoundedLastUpdate);

        HistoryArray ret = new()
        {
            DestinationId = destination,
            ParkId = park,
            ParkElementId = element,
            
            Begin = from,
            End = to,
            Period = period            
        };

        int historyPtr = 0;
        for(DateTime dt = from; dt < to; dt += period)
        {
            HistoryPoint hp = new()
            {
                Begin = dt,
                Duration = period,
            };

            List<Queue> LdForThisHour = new();
            while(true)
            {
                if (historyPtr >= lst.Count)
                    break;

                var current = lst[historyPtr];
                if(current.RoundedLastUpdate < from)
                {
                    historyPtr++;
                    continue;
                }
                    

                if (current.RoundedLastUpdate > to)
                    break;

                historyPtr++; //for later use, do not reuse in this while

                if (current.RoundedLastUpdate < dt || current.RoundedLastUpdate > dt + period)
                    break;

                if (current is not Queue)
                    continue;

                LdForThisHour.Add((Queue)current);
            }

            if (LdForThisHour.Count == 0)
                continue;

            var classicWaitingTimes = LdForThisHour.Where(x => x.ClassicWaitTime.HasValue).Select(x => x.ClassicWaitTime.Value.TotalMinutes);
            var operatingTimes = LdForThisHour.Select(x => x.Status == Status.OPENED ? 1.0 : 0.0);

            hp.SamplesCount = LdForThisHour.Count;
            hp.FirstMesure = LdForThisHour.Min(x => x.RoundedLastUpdate);
            hp.LastMesure = LdForThisHour.Max(x => x.RoundedLastUpdate);

            if(classicWaitingTimes.Any())
            {
                hp.MinimumWaitingTime = TimeSpan.FromMinutes(classicWaitingTimes.Min());
                hp.MaximumWaitingTime = TimeSpan.FromMinutes(classicWaitingTimes.Max());
                hp.AverageWaitingTime = TimeSpan.FromMinutes(classicWaitingTimes.Average());
            }

            if (operatingTimes.Any())
                hp.OperatingTime = operatingTimes.Average();

            ret.Points.Add(hp);
        }

        return ret;
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
                IsOrdered = false,
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

    public async Task<bool> CheckIsConnected(CancellationToken tk = default)
    {
        try
        {
            await MongoDatabase.RunCommandAsync((Command<BsonDocument>)"{ping:1}", cancellationToken:tk); ;

            return true;
        }
        catch(Exception e)
        {
            return false;
        }
    }

    public async Task<User?> GetUserFromContext(HttpContext ctx, CancellationToken tk = default)
    {
        if (ctx.User.Identity == null || ctx.User.Identity.Name == null)
            return null;

        var identity = ctx.User.Identity as ClaimsIdentity;
        if (identity == null)
            return null;

        var claimid = identity.FindFirst(ClaimTypes.Name);
        if (claimid == null)
            return null;

        string id = claimid.Value;

        var user = await DataStore.Instance.GetUser(id, tk);
        return user;
    }
}
