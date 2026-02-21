using System.Collections.Generic;

public readonly struct OnlineLootGenerated
{
    public int ActorId { get; }
    public string OwnerPlayerId { get; }
    public IReadOnlyList<LootItemSnapshot> Loot { get; }

    public OnlineLootGenerated(int actorId, string ownerPlayerId, IReadOnlyList<LootItemSnapshot> loot)
    {
        ActorId = actorId;
        OwnerPlayerId = ownerPlayerId ?? string.Empty;
        Loot = loot;
    }
}
