public sealed class MatchPauseService : IMatchPauseService
{
    private readonly IEventBus _eventBus;

    public bool IsPaused { get; private set; }
    public string Reason { get; private set; } = string.Empty;

    public MatchPauseService(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public void Pause(string reason)
    {
        IsPaused = true;
        Reason = reason ?? string.Empty;
        _eventBus.Publish(new MatchPauseStateChanged(true, Reason));
    }

    public void Resume()
    {
        if (!IsPaused)
        {
            return;
        }

        IsPaused = false;
        Reason = string.Empty;
        _eventBus.Publish(new MatchPauseStateChanged(false, string.Empty));
    }
}
