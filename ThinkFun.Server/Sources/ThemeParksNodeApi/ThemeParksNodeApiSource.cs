namespace ThinkFun.Server.Sources.ThemeParksNodeApi;

public class ThemeParksNodeApiSource
    : IDataSource
{
    public const string ExternalIdKey = "tpna";
    public const string CommonIdKey = "common";
    public bool UseParkPositionForDestinationIfUnknown = true;
    HttpClient Client;

    public ThemeParksNodeApiSource()
    {
        Client = new()
        {
            BaseAddress = new Uri("https://tp.arendz.nl/")
        };
    }

    public override Task UpdateLiveData(DataCollection collection, CancellationToken t)
    {
        throw new NotImplementedException();
    }

    public override Task UpdateStaticData(DataCollection collection, CancellationToken t)
    {
        throw new NotImplementedException();
    }
}
