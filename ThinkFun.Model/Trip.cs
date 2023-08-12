namespace ThinkFun.Model;

public class Trip
{
    public string Identifier { get; set; }
    public string Destination { get; set; }
    public List<string> Members { get; set; } = new();
}
