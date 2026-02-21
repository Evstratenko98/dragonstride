public readonly struct MultiplayerCreateSessionRequest
{
    public string Name { get; }
    public int MaxPlayers { get; }
    public bool IsPrivate { get; }
    public bool IsLocked { get; }
    public bool EnableRelayPreconnect { get; }
    public string Region { get; }

    public MultiplayerCreateSessionRequest(
        string name,
        int maxPlayers,
        bool isPrivate,
        bool isLocked,
        bool enableRelayPreconnect = true,
        string region = "")
    {
        Name = name;
        MaxPlayers = maxPlayers;
        IsPrivate = isPrivate;
        IsLocked = isLocked;
        EnableRelayPreconnect = enableRelayPreconnect;
        Region = region;
    }
}
