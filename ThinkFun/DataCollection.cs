using System.Net.Http.Json;
using ThinkFun.Model;

namespace ThinkFun;

public class DataCollection
{
    List<Destination> AllDestinations = null;
    HttpClient Client;

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

    public async Task<List<Destination>> GetDestinations(CancellationToken tk = default)
    {
        while (BufferedDestination.Count == 0 )
        {
            await Flush(tk);

            if (tk.IsCancellationRequested)
                return BufferedDestination;
        }

        return BufferedDestination;
    }

    public DataCollection() 
    {
        Client = new()
        {
            BaseAddress = new Uri("http://192.168.1.156:5000/")
        };
    }

    public async Task Flush(CancellationToken tk = default)
    {
        try
        {
            await FlushDestinations(tk);
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    public async Task FlushDestinations(CancellationToken tk = default)
    {
        AllDestinations = await Client.GetFromJsonAsync<List<Destination>>("Data/GetDestinations", tk);
    }
}
