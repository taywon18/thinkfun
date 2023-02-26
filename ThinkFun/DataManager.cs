using System.ComponentModel;
using System.Net.Http.Json;
using ThinkFun.Model;

namespace ThinkFun;

public class DataManager
{
    public static DataManager Instance = new DataManager();
    public DataCollection Data { get; } = new DataCollection();

    private string? DestinationId = null;

    public bool HasDestination
    {
        get
        {
            return DestinationId != null;
        }
    }


    public  void SetCurrentDestination(string destinationId)
    {
        DestinationId = destinationId;
    }
    

    private DataManager()
    {
    }

    public async Task UpdateDestinationData()
    {
    }
}
