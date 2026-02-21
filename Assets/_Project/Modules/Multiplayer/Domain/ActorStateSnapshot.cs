public readonly struct ActorStateSnapshot
{
    public int ActorId { get; }
    public string ActorType { get; }
    public string OwnerPlayerId { get; }
    public string CharacterId { get; }
    public string DisplayName { get; }
    public int CellX { get; }
    public int CellY { get; }
    public int Health { get; }
    public int Level { get; }
    public bool HasCrown { get; }
    public bool IsAlive { get; }

    public ActorStateSnapshot(
        int actorId,
        string actorType,
        string ownerPlayerId,
        string characterId,
        string displayName,
        int cellX,
        int cellY,
        int health,
        int level,
        bool hasCrown,
        bool isAlive)
    {
        ActorId = actorId;
        ActorType = actorType;
        OwnerPlayerId = ownerPlayerId;
        CharacterId = characterId;
        DisplayName = displayName;
        CellX = cellX;
        CellY = cellY;
        Health = health;
        Level = level;
        HasCrown = hasCrown;
        IsAlive = isAlive;
    }
}
