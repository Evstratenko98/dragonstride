public readonly struct MatchPauseStateChanged
{
    public bool IsPaused { get; }
    public string Reason { get; }

    public MatchPauseStateChanged(bool isPaused, string reason)
    {
        IsPaused = isPaused;
        Reason = reason;
    }
}
