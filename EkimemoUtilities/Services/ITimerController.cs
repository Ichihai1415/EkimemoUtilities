namespace EkimemoUtilities.Services;

public interface ITimerController
{
    bool IsRunning { get; }
    TimeSpan Remaining { get; }

    event Action<TimeSpan>? RemainingChanged;
    event Action? Completed;

    void Start(TimeSpan duration);
    void Stop();
}