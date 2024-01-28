using GeolocatorPlugin;

namespace ThinkFun;

public class LocationManager
{
    const double REFRESHING_CONTINUOUS_TIME_SECONDS = 0.2;
    const double REFRESHING_DEFAULT_MAX_TIME_BUFFERED_SECONDS = 60;
    const double REFRESHING_DEFAULT_MAX_TIME_UNBUFFERED_SECONDS = 5;

    public static LocationManager Instance { get; } = new LocationManager();
    public bool IsListening { get => CrossGeolocator.Current.IsListening; }

    private Mutex Mutex; //= new Mutex();

    GeolocatorPlugin.Abstractions.Position? LastPosition = null;
    DateTime LastPositionUpdated = DateTime.MinValue;
    public bool Allowed = false;
    public int Retry = 0;

    private LocationManager()
    {
        CrossGeolocator.Current.PositionChanged += CurrentPositionChanged;
    }

    public async Task StartListening()
    {
        await CrossGeolocator.Current.StartListeningAsync(TimeSpan.FromSeconds(REFRESHING_CONTINUOUS_TIME_SECONDS), 1, true);
    }

    public async Task StopListening()
    {
        await CrossGeolocator.Current.StopListeningAsync();
    }

    private void CurrentPositionChanged(object sender, GeolocatorPlugin.Abstractions.PositionEventArgs e)
    {
        LastPosition = e.Position;
        LastPositionUpdated = DateTime.UtcNow;
    }

    public async Task<GeolocatorPlugin.Abstractions.Position?> GetPositionBuffered(TimeSpan? maxtime = null, CancellationToken tk = default)
    {
        if (!maxtime.HasValue)
            maxtime = TimeSpan.FromSeconds(REFRESHING_DEFAULT_MAX_TIME_BUFFERED_SECONDS);

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

        GeolocatorPlugin.Abstractions.Position ret = null;
        try
        {
            ret = await CrossGeolocator.Current.GetPositionAsync(TimeSpan.FromSeconds(REFRESHING_DEFAULT_MAX_TIME_UNBUFFERED_SECONDS), tk, true);
        } 
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return null;
        }

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

        PermissionStatus status = default;
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            status = await Permissions.CheckStatusAsync<Permissions.LocationAlways>();
        });

        if (status == PermissionStatus.Granted)
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
