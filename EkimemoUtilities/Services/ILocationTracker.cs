namespace EkimemoUtilities.Services;

public interface ILocationTracker
{
    bool IsRunning { get; }
    LocationInfo? Last { get; }
    event Action<LocationInfo>? LocationChanged;
    event Action<bool>? RunningChanged;

    void Start();
    void Stop();
}