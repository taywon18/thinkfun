using System.Net.Http.Json;
using ThinkFun.Model;

namespace ThinkFun;

public class DataCollection
{
    public List<Destination> AllDestinations = new List<Destination>();
    HttpClient Client;

    public DataCollection() 
    {
        Client = new()
        {
            BaseAddress = new Uri("https://localhost:7035/")
        };
    }

    public async Task Flush()
    {
        try
        {
            await FlushDestinations();
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    public async Task FlushDestinations(CancellationToken tk = default)
    {

        var dest = await Client.GetFromJsonAsync<List<Destination>>("Data/GetDestinations", tk);

        lock (AllDestinations)
        {
            AllDestinations.Clear();
            AllDestinations.AddRange(dest);
        }
    }
}
