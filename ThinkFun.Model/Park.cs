namespace ThinkFun.Model;

public class Park
    : Base
{
    public string Name { get; set; } = "";
    public string ParentId { get; set; } = "";
    public Position Position { get; set; } = new Position();
}