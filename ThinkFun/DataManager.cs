using System.ComponentModel;
using System.Net.Http.Json;
using System.Text.Json;
using ThinkFun.Model;

namespace ThinkFun;

public class DataManager
{
    public static DataManager Instance = new DataManager();

    HttpClient Client;
    public Configuration Configuration { get; private set; }

    List<Destination> AllDestinations = null;
    List<Park> Parks { get; } = new List<Park>();
    List<ParkElement> Elements { get; } = new List<ParkElement>();
    List<LiveData> LiveDatas { get; } = new List<LiveData>();
    IDispatcherTimer UpdateTime, SaveConfigTimer;

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

    public List<ParkElement> BufferedElements
    {
        get
        {
            if (Elements == null)
                return new List<ParkElement>();

            List<ParkElement> ret;
            lock (Elements)
                ret = new List<ParkElement>(Elements);

            return ret;
        }
    }

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
        Client = new()
        {
            BaseAddress = new Uri("http://192.168.1.156:5000/")
        };

        LoadConfig();

        UpdateTime = Application.Current.Dispatcher.CreateTimer();
        UpdateTime.Interval = TimeSpan.FromSeconds(15);
        UpdateTime.Tick += async (s, e) => await Update();
        UpdateTime.IsRepeating = true;
        UpdateTime.Start();
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
            await Flush(tk);

            if (tk.IsCancellationRequested)
                return BufferedDestination;
        }

        return BufferedDestination;
    }

    public async Task Flush(CancellationToken tk = default)
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

        await UpdateStaticData();
        await UpdateDynamicData();
    }

    public async Task FlushDestinations(CancellationToken tk = default)
    {
        AllDestinations = await Client.GetFromJsonAsync<List<Destination>>("Data/GetDestinations", tk);
    }

    public async Task UpdateStaticData(CancellationToken tk = default)
    {
        if (DestinationId == null)
            return;
        string dest = DestinationId;

        var static_data = await Client.GetFromJsonAsync<StaticDestinationData>("Data/GetDestinationStaticData/" + dest, tk);
        lock (Parks)
        {
            Parks.Clear();
            Parks.AddRange(static_data.Parks);
        }

        lock (Elements)
        {
            Elements.Clear();
            Elements.AddRange(static_data.Restaurants);
            Elements.AddRange(static_data.Shows);
            Elements.AddRange(static_data.Attractions);
        }
    }

    public async Task UpdateDynamicData(CancellationToken tk = default)
    {
        if (DestinationId == null)
            return;
        string dest = DestinationId;

        var live_data = await Client.GetFromJsonAsync<LiveDestinationData>("Data/GetDestinationLiveData/" + dest, tk);
        lock (LiveDatas)
        {
            LiveDatas.Clear();
            LiveDatas.AddRange(live_data.Queues);
        }
    }
}
