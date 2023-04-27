using GeolocatorPlugin;

namespace ThinkFun;

public class LocationManager
{
    public static LocationManager Instance { get; } = new LocationManager();

    public bool Allowed = false;
    public int Retry = 0;

    private Mutex Mutex; //= new Mutex();

    GeolocatorPlugin.Abstractions.Position? LastPosition = null;
    
    DateTime LastPositionUpdated = DateTime.MinValue;

    private LocationManager()
    {
    }

    public async Task StartListening()
    {
        await CrossGeolocator.Current.StartListeningAsync(TimeSpan.FromSeconds(0.2), 1, true);
        CrossGeolocator.Current.PositionChanged += CurrentPositionChanged;
    }

    private void CurrentPositionChanged(object sender, GeolocatorPlugin.Abstractions.PositionEventArgs e)
    {
        LastPosition = e.Position;
        LastPositionUpdated = DateTime.UtcNow;
    }

    public async Task<GeolocatorPlugin.Abstractions.Position?> GetPositionBuffered(TimeSpan? maxtime = null, CancellationToken tk = default)
    {
        if (!maxtime.HasValue)
            maxtime = TimeSpan.FromSeconds(60);

        var elapsed = DateTime.Now - LastPositionUpdated;
        if (LastPosition != null && elapsed <= maxtime)
            return LastPosition;

        return await GetPositionAsync(tk);
    }

    public async Task<GeolocatorPlugin.Abstractions.Position?> GetPositionAsync(CancellationToken tk = default)
    {
        Mutex?.WaitOne();

        if (!await EnsureAllowed())
        {
            Mutex?.ReleaseMutex();
            return null;
        }
        CrossGeolocator.Current.DesiredAccuracy = 100;

        Mutex?.ReleaseMutex();
        var ret =  await CrossGeolocator.Current.GetPositionAsync(TimeSpan.FromSeconds(5), tk, true);

        Mutex?.WaitOne();
        if (ret.HasLatitudeLongitude)
        {
            LastPosition = ret;
            LastPositionUpdated = DateTime.Now;
        }
        Mutex?.ReleaseMutex();

        return ret;
    }

    public async Task<bool> EnsureAllowed()
    {
        if (Retry > 5)
            return Allowed;

        if (Allowed)
            return true;

        Retry++;
        PermissionStatus status = await Permissions.CheckStatusAsync<Permissions.LocationAlways>();
        if(status == PermissionStatus.Granted)
        {
            Allowed = true;
            return true;
        }

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            status = await Permissions.RequestAsync<Permissions.LocationAlways>();
        });

        if (status == PermissionStatus.Granted)
        {
            Allowed = true;
            return true;
        }

        return false;
    }

}
