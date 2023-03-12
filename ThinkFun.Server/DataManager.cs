using ThinkFun.Server.Sources;
using Wood;

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

        await UpdateStaticData();
        await UpdateLiveData();

        FlushLiveDataTimer = new System.Timers.Timer(60000);
        // Hook up the Elapsed event for the timer. 
        FlushLiveDataTimer.Elapsed += async (s, e) => {
            await UpdateLiveData();
        };
        FlushLiveDataTimer.AutoReset = true;
        FlushLiveDataTimer.Enabled = true;
    }

    public async Task UpdateStaticData()
    {
        var tks = new CancellationTokenSource();

        foreach(var source in sources)
            try
            {
                await source.UpdateStaticData(Data, tks.Token);
            }
            catch(Exception e)
            {
                LogManager.Error($"Catching error while updating static data in DataManager: {e}.");
            }
    }

    public async Task UpdateLiveData()
    {
        var tks = new CancellationTokenSource();

        DateTime before = DateTime.Now;
        foreach (var source in sources)
            try
            {
                await source.UpdateLiveData(Data, tks.Token);
            }
            catch (Exception e)
            {
                LogManager.Error($"Catching error while updating live data in DataManager: {e}.");
            }        
        DateTime after = DateTime.Now;
        var delta = after - before;

        if(FlushLiveDataTimer != null && FlushLiveDataTimer.Interval < delta.TotalMilliseconds)
            LogManager.Debug($"Live data update took {delta.TotalMilliseconds}ms, but the timer interval is {FlushLiveDataTimer.Interval}ms.");
        else
            LogManager.Debug($"Live data update took {delta.TotalMilliseconds}ms.");
    }

    public void Init()
    {
    }
}
