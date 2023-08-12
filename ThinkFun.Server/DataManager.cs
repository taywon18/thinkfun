using MongoDB.Driver.Core.Configuration;
using System;
using System.Text.Json;
using ThinkFun.Model;
using ThinkFun.Server.Sources;
using Wood;

namespace ThinkFun.Server;

public class DataManager
{
    private class Config
    {
        public HashSet<string>? DestinationFilter { get; set; } = new();

        public HashSet<string>? Providers { get; set; } = new();
    }

    public static DataManager Instance { get;  } = new DataManager();

    const double SaveTimeMinutes = 5;

    public DataCollection Data { get; } = new DataCollection();

    private static System.Timers.Timer? FlushLiveDataTimer;

    List<IDataSource> sources = new List<IDataSource>();

    public HashSet<string> DestinationFilter { private set; get; } = new();

    public DateTime? LastSave = null;

    private DataManager() 
    {
    }

    public async Task Configure(IConfiguration conf, CancellationToken tk = default)
    {
        var config = conf.Get<Config>();

        if (config == null)
        {
            LogManager.Critical("Cannot load Manager configuration.");
            return;
        }

        if(config.Providers == null || config.Providers.Count == 0)
        {
            LogManager.Critical("Not any source provided.");
            return;
        }

        foreach(var i in config.Providers)
            if(i.ToLower() == "themeparkwiki")
                sources.Add(new Sources.ThemeParkWiki.ThemeParkWikiSource());
            else
                LogManager.Critical($"Unknown source {i}.");

        if (config.DestinationFilter != null)
        {
            DestinationFilter.Clear();
            DestinationFilter = config.DestinationFilter;

            LogManager.Information($"Ignoring {String.Join(",",DestinationFilter)}.");
            return;
        }
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

        DateTime before = DateTime.Now;
        foreach (var source in sources)
            try
            {
                await source.UpdateStaticData(Data, tks.Token);
            }
            catch(Exception e)
            {
                LogManager.Error($"Catching error while updating static data in DataManager: {e}.");
            }
        var updateTime = DateTime.Now - before;

        LogManager.Debug($"Static data update took {updateTime}.");
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
        var updateTime = DateTime.Now - before;


        before = DateTime.Now;
        await SaveLiveDataIfNeeded();
        var saveTime = DateTime.Now - before;

        if (FlushLiveDataTimer != null && FlushLiveDataTimer.Interval < (updateTime + saveTime).TotalMilliseconds)
            LogManager.Warn($"Live data update and save took {(updateTime + saveTime).TotalSeconds}s ({updateTime.TotalMilliseconds}ms + {saveTime.TotalMilliseconds}ms), but the timer interval is {FlushLiveDataTimer.Interval/1000}s.");
        else
            LogManager.Debug($"Live data update and save took {(updateTime + saveTime).TotalSeconds}s ({updateTime.TotalMilliseconds}ms + {saveTime.TotalMilliseconds}ms).");
    }

    public async Task SaveLiveDataIfNeeded(CancellationToken tk = default)
    {
        if (LastSave != null && (DateTime.Now - LastSave.Value).TotalMinutes < SaveTimeMinutes)
            return;

        await SaveLiveData(tk);
    }

    public async Task SaveLiveData(CancellationToken tk = default)
    {
        await DataStore.Instance.Add(Data.AllLiveData, tk);
    }

    public async void PushEvent(Event e)
    {
        LogManager.Information($"New event {JsonSerializer.Serialize(e)}");
        await DataStore.Instance.Add(e);
    }
}
