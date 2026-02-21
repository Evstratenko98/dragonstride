public interface IMatchPauseService
{
    bool IsPaused { get; }
    string Reason { get; }

    void Pause(string reason);
    void Resume();
}
