using System.Net.Http.Json;
using ThinkFun.Model;

namespace ThinkFun;

public class DataManager
{
    public static DataManager Instance = new DataManager();
    public DataCollection Data { get; } = new DataCollection();

    

    private DataManager()
    {
    }

    public async Task UpdateDestinationData()
    {
    }
}
