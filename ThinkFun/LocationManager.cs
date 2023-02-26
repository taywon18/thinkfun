using GeolocatorPlugin;

namespace ThinkFun;

public class LocationManager
{
    public static LocationManager Instance { get; } = new LocationManager();

    public bool Allowed = false;

    private LocationManager()
    {
        
    }

    public async Task<GeolocatorPlugin.Abstractions.Position?> GetPositionAsync()
    {
        if (!await EnsureAllowed())
            return null;

        return await CrossGeolocator.Current.GetPositionAsync();
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
