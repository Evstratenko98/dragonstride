public readonly struct OnlinePlayerConnectionChanged
{
    public string PlayerId { get; }
    public bool IsConnected { get; }

    public OnlinePlayerConnectionChanged(string playerId, bool isConnected)
    {
        PlayerId = playerId;
        IsConnected = isConnected;
    }
}
