namespace ThinkFun.Server.Sources;

public interface IDataSource
{
    Task Update(DataCollection collection, CancellationToken t);
    Task UpdateLiveData(DataCollection collection, CancellationToken t);
}
