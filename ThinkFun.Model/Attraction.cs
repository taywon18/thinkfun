namespace ThinkFun.Model;

public class Attraction 
    : InterestPoint
{
    public enum AttractionType
    {
        Ride
    }

    public AttractionType? Type { get; set;}
}