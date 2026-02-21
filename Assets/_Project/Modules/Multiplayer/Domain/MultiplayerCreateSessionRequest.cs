public readonly struct MultiplayerCreateSessionRequest
{
    public string Name { get; }
    public int MaxPlayers { get; }
    public bool IsPrivate { get; }
    public bool IsLocked { get; }

    public MultiplayerCreateSessionRequest(string name, int maxPlayers, bool isPrivate, bool isLocked)
    {
        Name = name;
        MaxPlayers = maxPlayers;
        IsPrivate = isPrivate;
        IsLocked = isLocked;
    }
}
