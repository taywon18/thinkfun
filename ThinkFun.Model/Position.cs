namespace ThinkFun.Model;

public class Position
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public override string ToString()
    {
        return String.Format("({0},{1})", Latitude, Longitude);
    }
}
