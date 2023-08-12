namespace ThinkFun.Model;

public class InterestPoint
    : ParkElement
{
    public Position? Position { get; set; } = default;

    static public Position? FindBestPositionFor(InterestPoint what, IEnumerable<InterestPoint> children)
    {
        if(what != null && what.Position != null)
            return what.Position;

        List<Position> positions = new();
        foreach(var child in children)
            if(child.Position != null)
                positions.Add(child.Position);
        
        if(positions.Count == 0) 
            return null;

        return Position.MidCenter(positions);

    }
}
