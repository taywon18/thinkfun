using System.Text.Json;

namespace ThinkFun.Model;


public class ParkElement
    : Base
{
    public string Name { get; set; } = "";
    public string DestinationId { get; set; } = "";
    public string ParkId { get; set; } = "";

    public override int GetHashCode()
    {
        return base.UniqueIdentifier.GetHashCode();
    }

    public override bool Equals(object? obj)
    {

        if (obj == null)
            return false;

        if (this.GetType() != obj.GetType())
            return false;


        var objAsIp = obj as InterestPoint;
        if (objAsIp == null)
            return false;

        return base.UniqueIdentifier == objAsIp.UniqueIdentifier;
    }

    public override string ToString()
    {
        return String.Format("{0} ({1}: {2})", Name, this.GetType().Name, UniqueIdentifier);
    }
}
