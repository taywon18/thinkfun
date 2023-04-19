namespace ThinkFun.Server;
using MongoDB.Driver;


public class DataStore
{
    public static DataStore Instance { get;  } = new DataStore();

    public DataStore()
    {

    }

}
