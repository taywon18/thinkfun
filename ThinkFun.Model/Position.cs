namespace ThinkFun.Model;

public class Position
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public override string ToString()
    {
        return String.Format("latlong({0},{1})", Latitude.ToString(), Longitude.ToString());
    }

    public static Position MidCenter(IEnumerable<Position> geoCoordinates)
    {
        if(geoCoordinates == null)
            throw new ArgumentNullException(nameof(geoCoordinates));

        //source: Gio, https://stackoverflow.com/questions/6671183/calculate-the-center-point-of-multiple-latitude-longitude-coordinate-pairs
        var geoCoordinatesAsList = geoCoordinates.ToList();
        
        if (geoCoordinatesAsList.Count == 0)
            throw new ArgumentException();

        if (geoCoordinatesAsList.Count == 1)
        {
            return geoCoordinatesAsList.Single();
        }

        double x = 0;
        double y = 0;
        double z = 0;

        foreach (var geoCoordinate in geoCoordinatesAsList)
        {
            var latitude = geoCoordinate.Latitude * Math.PI / 180;
            var longitude = geoCoordinate.Longitude * Math.PI / 180;

            x += Math.Cos(latitude) * Math.Cos(longitude);
            y += Math.Cos(latitude) * Math.Sin(longitude);
            z += Math.Sin(latitude);
        }

        var total = geoCoordinatesAsList.Count;

        x = x / total;
        y = y / total;
        z = z / total;

        var centralLongitude = Math.Atan2(y, x);
        var centralSquareRoot = Math.Sqrt(x * x + y * y);
        var centralLatitude = Math.Atan2(z, centralSquareRoot);

        return new Position
        {
            Latitude = centralLatitude * 180 / Math.PI,
            Longitude = centralLongitude * 180 / Math.PI
        };
    }

    public bool IsZero()
    {
        return Latitude == 0 && Longitude == 0;
    }
}
