using GeolocatorPlugin;

namespace ThinkFun;

public class LocationManager
{
    public static LocationManager Instance { get; } = new LocationManager();

    public bool Allowed = false;
    private Mutex Mutex; //= new Mutex();

    private LocationManager()
    {
        
    }

    public async Task<GeolocatorPlugin.Abstractions.Position?> GetPositionAsync()
    {
        Mutex?.WaitOne();

        if (!await EnsureAllowed())
        {
            Mutex?.ReleaseMutex();
            return null;
        }

        Mutex?.ReleaseMutex();
        return await CrossGeolocator.Current.GetPositionAsync(TimeSpan.FromSeconds(2));
    }

    public async Task<bool> EnsureAllowed()
    {
        if (Allowed)
            return true;

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
