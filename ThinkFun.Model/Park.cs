namespace ThinkFun.Model;

public class Park
    : Base
{
    public string Name { get; set; } = "";
    public string DestinationId { get; set; } = "";
    public Position? Position { get; set; } = null;
}