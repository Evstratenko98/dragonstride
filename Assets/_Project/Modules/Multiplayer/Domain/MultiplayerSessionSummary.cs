public readonly struct MultiplayerSessionSummary
{
    public string SessionId { get; }
    public string Name { get; }
    public string HostPlayerId { get; }
    public int PlayerCount { get; }
    public int MaxPlayers { get; }
    public int AvailableSlots { get; }
    public bool IsLocked { get; }
    public bool HasPassword { get; }

    public MultiplayerSessionSummary(
        string sessionId,
        string name,
        string hostPlayerId,
        int playerCount,
        int maxPlayers,
        int availableSlots,
        bool isLocked,
        bool hasPassword)
    {
        SessionId = sessionId;
        Name = name;
        HostPlayerId = hostPlayerId;
        PlayerCount = playerCount;
        MaxPlayers = maxPlayers;
        AvailableSlots = availableSlots;
        IsLocked = isLocked;
        HasPassword = hasPassword;
    }
}
