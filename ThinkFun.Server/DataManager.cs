using ThinkFun.Server.Sources;

namespace ThinkFun.Server;

public class DataManager
{
    public static DataManager Instance { get;  } = new DataManager();
    public DataCollection Data { get; } = new DataCollection();

    private static System.Timers.Timer? FlushLiveDataTimer;

    List<IDataSource> sources = new List<IDataSource>();
    private DataManager() 
    {
        sources.Add(new Sources.ThemeParkWiki.ThemeParkWikiSource());
    }

    public async Task Start()
    {
        if (FlushLiveDataTimer != null)
            return;

        Console.WriteLine("Starting DataManager...");

        await Update();
        await UpdateLiveData();

        FlushLiveDataTimer = new System.Timers.Timer(15000);
        // Hook up the Elapsed event for the timer. 
        FlushLiveDataTimer.Elapsed += async (s, e) => {
            await UpdateLiveData();
        };
        FlushLiveDataTimer.AutoReset = true;
        FlushLiveDataTimer.Enabled = true;
    }

    public async Task Update()
    {
        var tks = new CancellationTokenSource();

        foreach(var source in sources)
            try
            {
                await source.Update(Data, tks.Token);
            }
            catch(Exception e)
            {
                Console.Write(e);
            }
    }

    public async Task UpdateLiveData()
    {
        var tks = new CancellationTokenSource();

        foreach (var source in sources)
            try
            {
                await source.UpdateLiveData(Data, tks.Token);
            }
            catch (Exception e)
            {
                Console.Write(e);
            }        
    }

    public void Init()
    {
    }
}
