using System.Collections.Generic;

public interface ILootSyncService
{
    bool HasPendingLootForActor(int actorId);

    MultiplayerOperationResult<IReadOnlyList<LootItemSnapshot>> GenerateLootForCell(int actorId, int cellX, int cellY);
    MultiplayerOperationResult<CharacterInventorySnapshot> ConfirmTakeLoot(int actorId);
    MultiplayerOperationResult<CharacterInventorySnapshot> AutoTakeLootOnEndTurn(int actorId);

    IReadOnlyList<ActionEventEnvelope> DrainPendingTimelineEvents();
}
