namespace ThinkFun.Server.Sources;

public abstract class IDataSource
{
    public bool FlushDestinations = true;
    public bool FlushParks = true;
    public bool FlushElements = true;

    public abstract Task UpdateStaticData(DataCollection collection, CancellationToken t);
    public abstract Task UpdateLiveData(DataCollection collection, CancellationToken t);
}
