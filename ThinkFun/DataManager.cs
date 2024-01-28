using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ThinkFun.Model;

namespace ThinkFun;

public class DataManager
{
    const string API_DATA_GET_DESTINATIONS = "Data/GetDestinations";
    const string API_DATA_DESTINATION_GET_STATIC_DATA = "Data/GetDestinationStaticData/";
    const string API_DATA_DESTINATION_GET_LIVE_DATA = "Data/GetDestinationLiveData/";
    const string API_DATA_DESTINATION_GET_LAST_EVENTS = "Data/GetLastEvents/";

    public static DataManager Instance { get; } = new DataManager();

    public HttpClientHandler HttpClientHandler { get; }
    public HttpClient Client { get; }

    public Configuration Configuration { get; private set; }

    List<Destination> AllDestinations = null;
    List<Park> Parks { get; } = new List<Park>();
    Dictionary<string, ParkElement> Elements { get; } = new ();
    List<LiveData> LiveDatas { get; } = new List<LiveData>();
    Dictionary<string, RichEvent> Events { get; } = new ();
    IDispatcherTimer UpdateTime, SaveConfigTimer;
    SemaphoreSlim UpdateLiveDataSemaphore = new SemaphoreSlim(1);
    SemaphoreSlim UpdateStaticDataSemaphore = new SemaphoreSlim(1);

    public string DestinationId
    {
        get { return Configuration.Destination; }
        set { 
            Configuration.Destination = value;
            SaveConfig();
        }
    }

    public bool HasDestination
    {
        get
        {
            return DestinationId != null;
        }
    }

    public List<Destination> BufferedDestination
    {
        get
        {
            if (AllDestinations == null)
                return new List<Destination>();

            List<Destination> ret;
            lock (AllDestinations)
                ret = new List<Destination>(AllDestinations);

            return ret;
        }
    }

    public List<Park> BufferedParks
    {
        get
        {
            if (Parks == null)
                return new List<Park>();

            List<Park> ret;
            lock (Parks)
                ret = new List<Park>(Parks);

            return ret;
        }
    }

    DateTime LastStaticUpdate = default;
    public List<ParkElement> BufferedElements
    {
        get
        {
            if (Elements == null)
                return new List<ParkElement>();

            List<ParkElement> ret;
            lock (Elements)
                ret = new List<ParkElement>(Elements.Values);

            return ret;
        }
    }

    public List<RichEvent> BufferedEvents
    {
        get
        {
            if (Events == null)
                return new();

            var ret = new List<RichEvent>();
            lock (Events)
                ret.AddRange(Events.Values);

            ret.Sort((a, b) => b.Event.Date.CompareTo(a.Event.Date));
            return ret;
        }
    }

    public async Task<List<ParkElement>> GetStaticDataBuffered(TimeSpan maxLive = default)
    {
        if (maxLive == default)
            maxLive = TimeSpan.FromMinutes(30);

        if (LastLiveUpdate == default || DateTime.Now - LastStaticUpdate > maxLive)
            await UpdateStaticData();

        return BufferedElements;
    }

    DateTime LastLiveUpdate = default;
    public Dictionary<string, LiveData> BufferedLiveDatas
    {
        get
        {
            if (LiveDatas == null)
                return new Dictionary<string, LiveData>();

            Dictionary<string, LiveData> ret = new Dictionary<string, LiveData> ();
            lock (LiveDatas)
                foreach(var i in LiveDatas)
                    ret.Add(i.ParkElementId, i);

            return ret;
        }
    }

    public async Task<Dictionary<string, LiveData>> GetLiveDataBuffered(TimeSpan maxLive = default)
    {
        if (maxLive == default)
            maxLive = TimeSpan.FromSeconds(15);

        if (LastLiveUpdate == default || DateTime.Now - LastLiveUpdate > maxLive)
            await UpdateDynamicData();

        return BufferedLiveDatas;
    }

    public  void SetCurrentDestination(string destinationId)
    {
        DestinationId = destinationId;

        if(Parks != null)
            lock(Parks)
                Parks.Clear();

        if (Parks != null)
            lock (Parks)
                Parks.Clear();
    }
    

    private DataManager()
    {
        HttpClientHandler = new()
        {
            UseCookies = true,
            AllowAutoRedirect = true,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };

        Client = new(HttpClientHandler)
        {
            BaseAddress = new Uri("http://voie93quarts.fr:5000/")
        };

        LoadConfig();
        LoadCookies();


        UpdateTime = Application.Current.Dispatcher.CreateTimer();
        UpdateTime.Interval = TimeSpan.FromSeconds(15);
        UpdateTime.Tick += async (s, e) => await Update();
        UpdateTime.IsRepeating = true;
        UpdateTime.Start();
    }

    private void LoadCookies()
    {
        string path = Path.Combine(FileSystem.Current.AppDataDirectory, "thinkfun.cookies");
        try
        {
            using var fs = File.OpenRead(path);
            var cookieCollection = JsonSerializer.Deserialize<CookieCollection>(fs);
            HttpClientHandler.CookieContainer.Add(cookieCollection);
        } catch(Exception ex)
        {

        }
    }

    public void SaveCookies()
    {
        string path = Path.Combine(FileSystem.Current.AppDataDirectory, "thinkfun.cookies");
        using var fs = File.OpenWrite(path);
        // Beware: GetAllCookies is available starting with .NET 6
        JsonSerializer.Serialize(fs, HttpClientHandler.CookieContainer.GetAllCookies());
    }

    private void LoadConfig()
    {
        string path = Path.Combine(FileSystem.Current.AppDataDirectory, "thinkfun.json");
        try
        {
            Configuration = JsonSerializer.Deserialize<Configuration>(File.ReadAllText(path));
            if (Configuration == null)
                throw new Exception();
        }catch(Exception ex)
        {
            Configuration = new Configuration();
        }
    }

    public void SaveConfig()
    {
        string path = Path.Combine(FileSystem.Current.AppDataDirectory, "thinkfun.json");
        try
        {
            File.WriteAllText(path, JsonSerializer.Serialize(Configuration));
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    public async Task<List<Destination>> GetDestinations(CancellationToken tk = default)
    {
        while (BufferedDestination.Count == 0)
        {
            await FlushSecurized(tk);

            if (tk.IsCancellationRequested)
                return BufferedDestination;
        }

        return BufferedDestination;
    }

    public async Task FlushSecurized(CancellationToken tk = default)
    {
        try
        {
            await FlushDestinations(tk);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    public async Task Update()
    {
        if(DestinationId  == null) return;

        await UpdateDynamicData();
        await UpdateEvents();
    }

    public async Task FlushDestinations(CancellationToken tk = default)
    {
        try 
        {
            AllDestinations = await Client.GetFromJsonAsync<List<Destination>>(API_DATA_GET_DESTINATIONS, tk);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    //todo: Optimize -_-'
    public ParkElement? GetParkElementById(string id)
    {
        lock(Elements)
        {
            if (Elements.TryGetValue(id, out var parkElement))
                return parkElement;
        }

        return null;
    }

    public async Task<bool> UpdateStaticData(CancellationToken tk = default)
    {
        if (DestinationId == null)
            return false;

        if (UpdateStaticDataSemaphore.CurrentCount == 0)
        {
            await UpdateStaticDataSemaphore.WaitAsync(tk);
            UpdateStaticDataSemaphore.Release();
            return false;
        }
        await UpdateStaticDataSemaphore.WaitAsync(tk);

        var dt = DateTime.Now;

        if (tk.IsCancellationRequested)
            return false;
            
        string dest = DestinationId;

        try
        {
            var static_data = await Client.GetFromJsonAsync<StaticDestinationData>(API_DATA_DESTINATION_GET_STATIC_DATA + dest, tk);

            lock (Parks)
            {
                Parks.Clear();
                Parks.AddRange(static_data.Parks);
            }

            lock (Elements)
            {
                Elements.Clear();
                foreach (var i in Enumerable.Union<ParkElement>(Enumerable.Union<ParkElement>(static_data.Restaurants, static_data.Shows), static_data.Attractions))
                    if (!Elements.TryAdd(i.UniqueIdentifier, i))
                        Console.WriteLine($"Cannot add element with the same id ({i.UniqueIdentifier}).");

                LastStaticUpdate = DateTime.Now;
            }
        }
        catch (Exception ex) {
            Console.WriteLine(ex);
        }

        Console.WriteLine($"Updated static data in {(DateTime.Now - dt).TotalMilliseconds}ms.");

        UpdateStaticDataSemaphore.Release();
        return true;
    }

    public async Task<bool> UpdateDynamicData(CancellationToken tk = default)
    {
        if (DestinationId == null)
            return false;

        if (UpdateLiveDataSemaphore.CurrentCount == 0)
        {
            await UpdateLiveDataSemaphore.WaitAsync(tk);
            UpdateLiveDataSemaphore.Release();
            return false;
        }
        await UpdateLiveDataSemaphore.WaitAsync(tk);

        if (tk.IsCancellationRequested)
            return false;

        var dt = DateTime.Now;

        string dest = DestinationId;

        try
        {
            var live_data = await Client.GetFromJsonAsync<LiveDestinationData>(API_DATA_DESTINATION_GET_LIVE_DATA + dest, tk);
            lock (LiveDatas)
            {
                LiveDatas.Clear();
                LiveDatas.AddRange(live_data.Queues);
                LastLiveUpdate = DateTime.Now;
            }
        }
        catch(Exception ex) 
        { 
            Console.WriteLine(ex);
        }

        Console.WriteLine($"Updated dynamic data in { (DateTime.Now - dt).TotalMilliseconds }ms.");

        UpdateLiveDataSemaphore.Release();
        return true;
    }

    public async Task UpdateEvents(CancellationToken tk = default)
    {
        string dest = DestinationId;

        try
        {
            var events_data = await Client.GetFromJsonAsync<EventsDestinationData>(API_DATA_DESTINATION_GET_LAST_EVENTS + dest, tk);
            TryPush(events_data.StatusEvents);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    public void TryPush(IEnumerable<Event> events)
    {
        lock(Events)
        {
            if (Events.Count == 0)
            {
                foreach (var i in events)
                    Events.TryAdd(i.UniqueId, new RichEvent(i));
                return;
            }

            if (DataManager.Instance.Elements.Count == 0)
                return;

            foreach(var i in events)
                if(Events.TryAdd(i.UniqueId, new RichEvent(i)))
                    onNewEvent(i);
        }
    }

    private void onNewEvent(Event e)
    {
        if(e is StatusChangedEvent sce)
        {
            string attractionName = DataManager.Instance.GetParkElementById(e.ParkElementId).Name;
            if(attractionName == null)
            {
                Console.WriteLine($"Unknown id {e.ParkElementId}.");
                return;
            }

            string statut = sce.NewStatus == Status.OPENED ? "d'ouvrir" : "de fermer";
            NotificationService.Instance.Notify($"{attractionName} vient {statut}.", $"{attractionName} vient {statut}.");
        }
        
    }
}
