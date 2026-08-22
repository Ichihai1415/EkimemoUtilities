namespace EkimemoUtilities.Services;

public interface ILocationTracker
{
    bool IsRunning { get; }
    LocationInfo? Last { get; }
    event Action<LocationInfo>? LocationChanged;

    void Start();
    void Stop();
}