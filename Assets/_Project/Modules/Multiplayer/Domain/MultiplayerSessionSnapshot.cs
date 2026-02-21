public readonly struct MultiplayerSessionSnapshot
{
    public string SessionId { get; }
    public string SessionCode { get; }
    public string Name { get; }
    public string HostPlayerId { get; }
    public int PlayerCount { get; }
    public int MaxPlayers { get; }
    public int AvailableSlots { get; }
    public bool IsHost { get; }
    public bool IsLocked { get; }
    public bool IsPrivate { get; }
    public bool HasPassword { get; }

    public MultiplayerSessionSnapshot(
        string sessionId,
        string sessionCode,
        string name,
        string hostPlayerId,
        int playerCount,
        int maxPlayers,
        int availableSlots,
        bool isHost,
        bool isLocked,
        bool isPrivate,
        bool hasPassword)
    {
        SessionId = sessionId;
        SessionCode = sessionCode;
        Name = name;
        HostPlayerId = hostPlayerId;
        PlayerCount = playerCount;
        MaxPlayers = maxPlayers;
        AvailableSlots = availableSlots;
        IsHost = isHost;
        IsLocked = isLocked;
        IsPrivate = isPrivate;
        HasPassword = hasPassword;
    }
}
