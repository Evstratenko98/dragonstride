using UnityEngine;

public readonly struct GameCommandEnvelope
{
    public long CommandId { get; }
    public GameCommandType CommandType { get; }
    public string PlayerId { get; }
    public Vector2Int Direction { get; }
    public int TargetActorId { get; }
    public int ClientTick { get; }

    public GameCommandEnvelope(
        long commandId,
        GameCommandType commandType,
        string playerId,
        Vector2Int direction,
        int targetActorId,
        int clientTick)
    {
        CommandId = commandId;
        CommandType = commandType;
        PlayerId = playerId;
        Direction = direction;
        TargetActorId = targetActorId;
        ClientTick = clientTick;
    }
}
