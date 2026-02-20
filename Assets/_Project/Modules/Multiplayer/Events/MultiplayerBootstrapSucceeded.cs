public readonly struct MultiplayerBootstrapSucceeded
{
    public string PlayerId { get; }

    public MultiplayerBootstrapSucceeded(string playerId)
    {
        PlayerId = playerId;
    }
}
